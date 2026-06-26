/**
 * 滑块验证码 JS 互操作：通过 fetch 调用 Admin 项目代理端点
 */
window.fetchCaptcha = async function () {
    const response = await fetch('/auth/captcha/slider');
    if (!response.ok) {
        throw new Error('验证码请求失败: ' + response.status);
    }
    return await response.text();
};

window.verifyCaptcha = async function (captchaId, sliderX) {
    const response = await fetch('/auth/captcha/slider/verify', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ captchaId: captchaId, sliderX: sliderX })
    });
    if (!response.ok) {
        throw new Error('验证请求失败: ' + response.status);
    }
    return await response.text();
};
