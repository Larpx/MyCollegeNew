using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Auth;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Distributed;
using SkiaSharp;
using System.Reflection;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Auth.Captcha
{
    /// <summary>
    /// 滑块验证码端点映射
    /// </summary>
    public static class CaptchaEndpoints
    {
        /// <summary>缓存键前缀：验证码位置</summary>
        private const string CacheKeyPrefix = "captcha:slider:";

        /// <summary>缓存键前缀：验证通过的 token</summary>
        private const string TokenCacheKeyPrefix = "captcha:token:";

        /// <summary>背景图宽度</summary>
        private const int ImageWidth = 300;

        /// <summary>背景图高度</summary>
        private const int ImageHeight = 150;

        /// <summary>拼图块大小</summary>
        private const int PuzzleSize = 44;

        /// <summary>拼图块凸起半径</summary>
        private const int PuzzleBumpRadius = 6;

        /// <summary>位置误差容忍（像素）</summary>
        private const int Tolerance = 5;

        /// <summary>缓存过期时间（分钟）</summary>
        private const int CacheExpirationMinutes = 5;

        /// <summary>嵌入资源中默认背景图的逻辑名称</summary>
        private const string DefaultBackgroundResourceName = "captcha-bg-default.jpg";

        /// <summary>
        /// 默认背景图（懒加载，全进程共享只读实例）
        /// </summary>
        private static readonly Lazy<SKBitmap?> _defaultBackground = new(LoadDefaultBackground);

        /// <summary>
        /// 从嵌入资源加载默认背景图
        /// </summary>
        /// <returns>解码后的 SKBitmap，加载失败返回 null</returns>
        private static SKBitmap? LoadDefaultBackground()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(DefaultBackgroundResourceName);
            if (stream is null)
            {
                return null;
            }

            return SKBitmap.Decode(stream);
        }

        /// <summary>
        /// 映射滑块验证码相关端点
        /// </summary>
        /// <param name="group">路由组</param>
        public static RouteGroupBuilder MapCaptchaEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/captcha/slider", GenerateSliderCaptcha)
                .WithName("GetSliderCaptcha")
                .WithSummary("生成滑块验证码")
                .AllowAnonymous()
                .CacheOutput(p => p.Expire(TimeSpan.FromSeconds(0)))
                .Produces<ApiResponse<SliderCaptchaResponse>>(StatusCodes.Status200OK);

            group.MapPost("/captcha/slider/verify", VerifySliderCaptcha)
                .WithName("VerifySliderCaptcha")
                .WithSummary("校验滑块验证码")
                .AllowAnonymous()
                .Produces<ApiResponse<SliderCaptchaVerifyResponse>>(StatusCodes.Status200OK);

            return group;
        }

        /// <summary>
        /// 生成滑块验证码图片
        /// </summary>
        private static async Task<IResult> GenerateSliderCaptcha(
            IDistributedCache cache,
            ILogger<Program> logger)
        {
            var captchaId = Guid.NewGuid().ToString("N");
            var random = Random.Shared;

            // 拼图缺口的 X 位置（60-220 之间，确保两侧有足够空间）
            var targetX = random.Next(60, ImageWidth - PuzzleSize - 40);
            var puzzleY = ImageHeight / 2 - PuzzleSize / 2;

            // 预渲染一份背景画布（背景图 + 拼图缺口）
            using var backgroundSurface = SKSurface.Create(new SKImageInfo(ImageWidth, ImageHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
            var bgCanvas = backgroundSurface.Canvas;
            bgCanvas.Clear(SKColors.White);

            // 绘制默认背景图（缩放裁剪至 300x150），失败时回退到随机渐变
            DrawBackground(bgCanvas, random);

            // 在背景图上绘制拼图缺口（半透明遮罩 + 边框）
            DrawPuzzleHole(bgCanvas, targetX, puzzleY);

            using var backgroundImage = backgroundSurface.Snapshot();
            using var bgData = backgroundImage.Encode(SKEncodedImageFormat.Png, 80);
            var bgBase64 = Convert.ToBase64String(bgData.AsSpan());

            // 生成滑块图：全尺寸 300×150 透明画布，仅拼图块区域有像素
            // 这样与背景图同尺寸，前端 CSS 渲染 1:1 对应，位置和大小完全匹配
            using var sliderSurface = SKSurface.Create(new SKImageInfo(ImageWidth, ImageHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
            var sliderCanvas = sliderSurface.Canvas;
            sliderCanvas.Clear(SKColors.Transparent);

            // 用拼图路径做蒙版，只保留拼图块区域内的背景像素
            using (var paint = new SKPaint { IsAntialias = true })
            {
                var puzzlePath = BuildPuzzlePath(targetX, puzzleY);
                sliderCanvas.ClipPath(puzzlePath, SKClipOperation.Intersect, true);
                // 从未标记缺口的原始背景中取色（重新绘制一次不含缺口阴影的背景）
                using var cleanSurface = SKSurface.Create(new SKImageInfo(ImageWidth, ImageHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
                var cleanCanvas = cleanSurface.Canvas;
                cleanCanvas.Clear(SKColors.White);
                DrawBackground(cleanCanvas, random);
                using var cleanImage = cleanSurface.Snapshot();
                sliderCanvas.DrawImage(cleanImage, 0, 0, SKSamplingOptions.Default);
            }

            // 为拼图块添加白色描边，提升可辨识度
            using var borderPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                Color = new SKColor(255, 255, 255, 220)
            };
            sliderCanvas.DrawPath(BuildPuzzlePath(targetX, puzzleY), borderPaint);

            using var sliderImage = sliderSurface.Snapshot();
            using var sliderData = sliderImage.Encode(SKEncodedImageFormat.Png, 80);
            var sliderBase64 = Convert.ToBase64String(sliderData.AsSpan());

            // 将正确位置存入缓存，5 分钟过期
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
            };
            await cache.SetStringAsync(
                CacheKeyPrefix + captchaId,
                targetX.ToString(),
                cacheOptions);

            logger.LogDebug("生成滑块验证码：{CaptchaId}，目标位置：{TargetX}", captchaId, targetX);

            return Results.Ok(ApiResponse<SliderCaptchaResponse>.Success(new SliderCaptchaResponse
            {
                CaptchaId = captchaId,
                BackgroundImage = "data:image/png;base64," + bgBase64,
                SliderImage = "data:image/png;base64," + sliderBase64,
                SliderX = 0
            }));
        }

        /// <summary>
        /// 绘制验证码背景：优先使用嵌入的默认背景图（缩放裁剪至目标尺寸），
        /// 加载失败时回退到随机渐变 + 噪点。
        /// </summary>
        /// <param name="canvas">目标画布</param>
        /// <param name="random">随机数生成器（回退时使用）</param>
        private static void DrawBackground(SKCanvas canvas, Random random)
        {
            var defaultBg = _defaultBackground.Value;
            if (defaultBg is not null)
            {
                // 等比缩放并居中裁剪，填满 300x150 区域
                var srcRect = ComputeCropRect(defaultBg.Width, defaultBg.Height, ImageWidth, ImageHeight);
                var destRect = new SKRect(0, 0, ImageWidth, ImageHeight);
                // SkiaSharp 4.x 弃用了 DrawBitmap+SKPaint 重载，改用 DrawImage + SKSamplingOptions
                using var defaultImg = SKImage.FromBitmap(defaultBg);
                canvas.DrawImage(defaultImg, srcRect, destRect, SKSamplingOptions.Default);
                return;
            }

            // 回退：随机渐变 + 噪点
            DrawRandomBackground(canvas, random, ImageWidth, ImageHeight);
        }

        /// <summary>
        /// 计算等比裁剪矩形：从源图中裁剪出与目标宽高比一致的最大区域
        /// </summary>
        /// <param name="srcW">源图宽度</param>
        /// <param name="srcH">源图高度</param>
        /// <param name="dstW">目标宽度</param>
        /// <param name="dstH">目标高度</param>
        /// <returns>源图中的裁剪矩形</returns>
        private static SKRect ComputeCropRect(int srcW, int srcH, int dstW, int dstH)
        {
            var srcRatio = (double)srcW / srcH;
            var dstRatio = (double)dstW / dstH;

            int cropW, cropH, cropX, cropY;
            if (srcRatio > dstRatio)
            {
                // 源图更宽：按高度裁剪，左右各切一部分
                cropH = srcH;
                cropW = (int)(srcH * dstRatio);
                cropX = (srcW - cropW) / 2;
                cropY = 0;
            }
            else
            {
                // 源图更高：按宽度裁剪，上下各切一部分
                cropW = srcW;
                cropH = (int)(srcW / dstRatio);
                cropX = 0;
                cropY = (srcH - cropH) / 2;
            }

            return new SKRect(cropX, cropY, cropX + cropW, cropY + cropH);
        }

        /// <summary>
        /// 校验滑块验证码
        /// </summary>
        private static async Task<IResult> VerifySliderCaptcha(
            SliderCaptchaVerifyRequest request,
            IDistributedCache cache,
            ILogger<Program> logger)
        {
            var cacheKey = CacheKeyPrefix + request.CaptchaId;

            // 从缓存读取正确位置
            var cachedValue = await cache.GetStringAsync(cacheKey);

            // 无论成功失败，都删除对应 CaptchaId 的缓存（一次性使用）
            await cache.RemoveAsync(cacheKey);

            if (string.IsNullOrEmpty(cachedValue) || !int.TryParse(cachedValue, out var targetX))
            {
                logger.LogWarning("滑块验证码已过期或不存在：{CaptchaId}", request.CaptchaId);
                return Results.Ok(ApiResponse<SliderCaptchaVerifyResponse>.Success(
                    new SliderCaptchaVerifyResponse
                    {
                        Success = false,
                        ErrorMessage = "验证码已过期，请重新获取"
                    }));
            }

            var diff = Math.Abs(request.SliderX - targetX);
            if (diff > Tolerance)
            {
                logger.LogWarning("滑块验证码校验失败：{CaptchaId}，误差：{Diff}px", request.CaptchaId, diff);
                return Results.Ok(ApiResponse<SliderCaptchaVerifyResponse>.Success(
                    new SliderCaptchaVerifyResponse
                    {
                        Success = false,
                        ErrorMessage = "验证失败，请重试"
                    }));
            }

            // 验证通过，颁发一次性 token
            var token = Guid.NewGuid().ToString("N");
            var tokenCacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
            };
            await cache.SetStringAsync(
                TokenCacheKeyPrefix + token,
                "1",
                tokenCacheOptions);

            logger.LogInformation("滑块验证码校验通过：{CaptchaId}，颁发 token", request.CaptchaId);

            return Results.Ok(ApiResponse<SliderCaptchaVerifyResponse>.Success(
                new SliderCaptchaVerifyResponse
                {
                    Success = true,
                    Token = token
                }));
        }

        /// <summary>
        /// 验证滑块验证码 token 的有效性（供 LoginHandler 调用）
        /// </summary>
        /// <param name="token">验证码 token</param>
        /// <param name="cache">分布式缓存</param>
        /// <returns>验证通过返回 true，否则 false</returns>
        public static async Task<bool> ValidateCaptchaTokenAsync(string token, IDistributedCache cache)
        {
            var tokenCacheKey = TokenCacheKeyPrefix + token;
            var cachedValue = await cache.GetStringAsync(tokenCacheKey);

            // 无论成功失败，删除缓存 token（一次性使用）
            await cache.RemoveAsync(tokenCacheKey);

            return !string.IsNullOrEmpty(cachedValue);
        }

        /// <summary>
        /// 绘制随机渐变/噪点背景
        /// </summary>
        private static void DrawRandomBackground(SKCanvas canvas, Random random, int width, int height)
        {
            // 随机渐变底色
            var color1 = new SKColor(
                (byte)random.Next(100, 200),
                (byte)random.Next(100, 200),
                (byte)random.Next(100, 200));
            var color2 = new SKColor(
                (byte)random.Next(130, 230),
                (byte)random.Next(130, 230),
                (byte)random.Next(130, 230));

            using var gradientPaint = new SKPaint
            {
                IsAntialias = true,
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    new SKPoint(width, height),
                    [color1, color2],
                    SKShaderTileMode.Clamp)
            };
            canvas.DrawRect(0, 0, width, height, gradientPaint);

            // 添加随机噪点
            using var noisePaint = new SKPaint { IsAntialias = false };
            for (var i = 0; i < 2000; i++)
            {
                var x = random.Next(width);
                var y = random.Next(height);
                var alpha = (byte)random.Next(20, 80);
                var gray = (byte)random.Next(100, 255);
                noisePaint.Color = new SKColor(gray, gray, gray, alpha);
                canvas.DrawPoint(x, y, noisePaint);
            }

            // 添加随机线条干扰
            using var linePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1
            };
            for (var i = 0; i < 5; i++)
            {
                linePaint.Color = new SKColor(
                    (byte)random.Next(80, 200),
                    (byte)random.Next(80, 200),
                    (byte)random.Next(80, 200),
                    (byte)random.Next(30, 80));
                canvas.DrawLine(
                    random.Next(width), random.Next(height),
                    random.Next(width), random.Next(height),
                    linePaint);
            }
        }

        /// <summary>
        /// 绘制拼图缺口（在背景图上挖空并添加阴影）
        /// </summary>
        private static void DrawPuzzleHole(SKCanvas canvas, int x, int y)
        {
            var path = BuildPuzzlePath(x, y);

            // 缺口半透明遮罩
            using var holePaint = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(0, 0, 0, 60)
            };
            canvas.DrawPath(path, holePaint);

            // 缺口边框
            using var borderPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f,
                Color = new SKColor(255, 255, 255, 120)
            };
            canvas.DrawPath(path, borderPaint);
        }

        /// <summary>
        /// 构建拼图块路径（圆形凸起样式）
        /// </summary>
        /// <param name="x">拼图块左上角 X</param>
        /// <param name="y">拼图块左上角 Y</param>
        /// <returns>拼图块路径</returns>
        private static SKPath BuildPuzzlePath(int x, int y)
        {
            var builder = new SKPathBuilder();
            var s = PuzzleSize;
            var r = PuzzleBumpRadius;

            // 从左上角开始，顺时针绘制
            builder.MoveTo(x, y);

            // 上边 + 右侧凸起
            builder.LineTo(x + s / 2 - r, y);
            builder.ArcTo(
                new SKRect(x + s / 2 - r, y - r, x + s / 2 + r, y + r),
                180, 180, false);
            builder.LineTo(x + s, y);

            // 右边 + 下方凸起
            builder.LineTo(x + s, y + s / 2 - r);
            builder.ArcTo(
                new SKRect(x + s - r, y + s / 2 - r, x + s + r, y + s / 2 + r),
                270, 180, false);
            builder.LineTo(x + s, y + s);

            // 下边
            builder.LineTo(x, y + s);

            // 左边
            builder.LineTo(x, y);

            builder.Close();
            return builder.Detach();
        }
    }
}
