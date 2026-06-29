// 二维码扫码互操作：使用浏览器原生 BarcodeDetector API
// 兼容 Chrome 83+ / Edge 83+ / Android Chrome，不支持时返回错误提示
window.qrScanner = {
    _stream: null,
    _detecting: false,
    _barcodeDetector: null,

    /// 检测浏览器是否支持扫码
    isSupported: function () {
        return 'BarcodeDetector' in window &&
            navigator.mediaDevices &&
            navigator.mediaDevices.getUserMedia;
    },

    /// 启动摄像头扫码
    /// videoElementId: video 元素的 id
    /// dotNetRef: DotNetObjectReference 引用，扫码成功后调用 OnQrCodeScanned(text)
    start: async function (videoElementId, dotNetRef) {
        const video = document.getElementById(videoElementId);
        if (!video) {
            return { success: false, error: '视频元素未找到' };
        }

        // 先停止已有的流
        this.stop();

        try {
            this._stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: 'environment' }
            });
            video.srcObject = this._stream;
            await video.play();

            if ('BarcodeDetector' in window) {
                this._barcodeDetector = new BarcodeDetector({ formats: ['qr_code'] });
                this._detecting = true;
                this._detect(video, dotNetRef);
                return { success: true, error: '' };
            } else {
                this.stop();
                return { success: false, error: '当前浏览器不支持原生扫码，请使用 Chrome 或 Edge 浏览器，或手动输入签到码' };
            }
        } catch (e) {
            this.stop();
            return { success: false, error: e.message || '无法访问摄像头，请检查浏览器权限设置' };
        }
    },

    /// 持续检测二维码
    _detect: async function (video, dotNetRef) {
        if (!this._detecting) return;

        try {
            const codes = await this._barcodeDetector.detect(video);
            if (codes && codes.length > 0) {
                const text = codes[0].rawValue;
                if (text) {
                    this._detecting = false;
                    this.stop();
                    await dotNetRef.invokeMethodAsync('OnQrCodeScanned', text);
                    return;
                }
            }
        } catch (e) {
            // 单帧检测出错，继续尝试下一帧
        }

        if (this._detecting) {
            requestAnimationFrame(() => this._detect(video, dotNetRef));
        }
    },

    /// 停止摄像头和检测
    stop: function () {
        this._detecting = false;
        if (this._stream) {
            this._stream.getTracks().forEach(function (t) { t.stop(); });
            this._stream = null;
        }
        this._barcodeDetector = null;
    }
};
