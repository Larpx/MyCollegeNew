(function () {
    var KasperskyLab = (function (ns) { ns.ContentSecurityPolicyNonceAttribute = 'E114306E99544CCE86571A8774BF2DF4'; return ns; })(KasperskyLab || {});
    var KasperskyLab = function (g) {
        function k(a) { return "undefined" === typeof a ? "undefined" : null === a ? "null" : Object.prototype.toString.call(a).match(/^\[object\s(.*)\]$/)[1] } function e(a) {
            if (null === a || Infinity == a || -Infinity == a || a === h) return "null"; var d = k(a); if ("Boolean" == d) return "" + a; if ("Number" == d) return window.isNaN(a) ? "null" : "" + a; if ("String" == d) return '"' + a + '"'; if ("object" == typeof a) {
                e.check || (e.check = []); for (var c = 0, b = e.check.length; c < b; ++c) if (e.check[c] === a) throw new TypeError; e.check.push(a); c = ""; if ("Array" ==
                d) { for (var d = 0, f = a.length; d < f; ++d) c += e(a[d]) + ","; e.check.pop(); return "[" + c.slice(0, -1) + "]" } for (f in a) a.hasOwnProperty(f) && (c += '"' + f + '":' + e(a[f]) + ","); e.check.pop(); return "{" + c.slice(0, -1) + "}"
            } return h
        } var h, l = {
            source: null, b: /^[\x20\t\n\r]*(?:([,:\[\]{}]|true|false|null)|(-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?)|"((?:[^\r\n\t\\\"]|\\(?:["\\\/trnfb]|u[0-9a-fA-F]{4}))*)")/, a: function () { throw new SyntaxError("JSON syntax error"); }, c: function (a) { this.source = a.input.slice(a[0].length); return this.b.exec(this.source) },
            f: function () { for (var a = this.b.exec(this.source), d = a && "]" != a[1], c = []; ; a = this.c(a)) { a || this.a(); if (d) c.push(this.d(a)), a = this.b.exec(this.source); else if (a[1]) if ("]" == a[1]) break; else "," != a[1] && this.a(); else this.a(); d = !d } return c }, g: function () {
                for (var a, d = !0, c = {}, b = this.b.exec(this.source) ; ; b = this.c(b)) if (b || this.a(), d) { if (b[1] && "}" == b[1]) break; else (b[1] || b[2] || !b[3]) && this.a(); a = b[3]; (b = this.c(b)) && b[1] && ":" == b[1] || this.a(); d = !1 } else {
                    a || this.a(); c[a] = this.d(b); b = this.c(this.b.exec(this.source)); if (b[1]) if ("}" ==
                    b[1]) break; else "," != b[1] && this.a(); else this.a(); a = h; d = !0
                } return c
            }, d: function (a) { if (a[1]) switch (a[1]) { case "[": return this.source = this.source.slice(a[0].length), this.f(); case "{": return this.source = this.source.slice(a[0].length), this.g(); case "true": return !0; case "false": return !1; case "null": return null; default: this.a() } else if (a[2]) return +a[2]; return a[3].replace(/\\(?:u(.{4})|(["\\\/\']))/g, function (a, c, b) { return c ? String.fromCharCode(parseInt(c, 16)) : b }) }, e: function (a) {
                if ("String" != k(a)) throw new TypeError;
                this.source = a; (a = this.b.exec(this.source)) || this.a(); return this.d(a)
            }
        }; g.JSONStringify = function (a) { return e(a) }; g.JSONParse = function (a) { return l.e(a) }; return g
    }.call(this, KasperskyLab || {});
    var KasperskyLab = function (a) {
        a.MaxRequestDelay = 2E3; a.Log = function (d) { }; a.GetResourceSrc = function (d) { return "/4FD2FB4778A17568ECC44599E603411E" + d }; a.AddEventListener = function (d, a, c) { "addEventListener" in d ? d.addEventListener(a, function (d) { c(d || window.event) }, !1) : d.attachEvent("on" + a, function (a) { c.call(d, a || window.event) }) }; a.AddRemovableEventListener = function (d, a, c) { d.addEventListener ? d.addEventListener(a, c, !1) : d.attachEvent("on" + a, c) }; a.RemoveEventListener = function (a, f, c) {
            a.removeEventListener ? a.removeEventListener(f,
            c, !1) : a.detachEvent("on" + f, c)
        }; a.AddStyles = function (a) { if ("object" == typeof a && a.constructor == Array) { var f = document.createElement("style"); f.type = "text/css"; f.setAttribute("nonce", KasperskyLab.ContentSecurityPolicyNonceAttribute); for (var c = 0, k = a.length; c < k; ++c) { var h = f, m = a[c]; h.styleSheet ? h.styleSheet.cssText += m + "\n" : h.appendChild(document.createTextNode(m)) } document.head ? document.head.appendChild(f) : document.getElementsByTagName("head")[0].appendChild(f) } }; a.GetCurrentTime = function () { return (new Date).getTime() };
        a.GetPageScroll = function () { return { left: document.documentElement && document.documentElement.scrollLeft || document.body.scrollLeft, top: document.documentElement && document.documentElement.scrollTop || document.body.scrollTop } }; a.GetPageHeight = function () { return document.documentElement.clientHeight || document.body.clientHeight }; a.GetPageWidth = function () { return document.documentElement.clientWidth || document.body.clientWidth }; return a
    }(KasperskyLab || {}), KasperskyLab = function (a) {
        a.Balloon = function (d, f, c, k) {
            function h() {
                l &&
                clearTimeout(l); l = 0
            } function m() { h.call(p) } function s(b) { l || (h(), l = setTimeout(function () { n() }, 200)) } function t(b) { k && k(b || window.event) && n.call(p) } function u() {
                KasperskyLab.AddStyles([".kisb * { position: relative; display:block; overflow-x:hidden; width: auto; margin:0; padding:0; font-family: Verdana; line-height: 150%; text-indent:0; border:0; text-align:left; box-sizing:content-box; letter-spacing: normal;}", ".kisb { z-index:2147483647; width: 280px; cursor:default; display:block;}", ".kisb a { text-decoration: underline; display:inline-block; }",
                ".kisb a:hover { text-decoration: none; }", ".kisb a, .kisb a:link, .kisb a:hover, .kisb a:visited { color: #008ccc;}"]); var b = document.createElement("div"); b.className = "kisb"; b.id = q; a.AddEventListener(b, "mouseout", s); a.AddEventListener(b, "mouseover", m); a.AddEventListener(b, "click", t); c(b); b.style.visibility = "hidden"; document.body.appendChild(b); return b
            } function v(b) {
                b = document.attachEvent && document.documentMode && 5 == document.documentMode ? {
                    width: b.clientWidth || b.scrollWidth, height: b.clientHeight ||
                    b.scrollHeight
                } : b.getBoundingClientRect(); return { width: b.width ? b.width : b.right - b.left, height: b.height ? b.height : b.bottom - b.top }
            } function n() { g && (g.style.visibility = "hidden") } function r(b, a, d) {
                (g = document.getElementById(q)) || (g = u()); if ("visible" != g.style.visibility) {
                    var c = 0, e = 0, e = v(g); if (1 == f) c = b, e = a - (e.height ? e.height : 20); else if (2 == f) { var c = KasperskyLab.GetPageWidth(), k = e.width / 2, c = k > b ? 0 : k + b > c ? c - e.width : b - k; b = KasperskyLab.GetPageHeight(); e = a + e.height > b ? a - e.height : a } else c = b, e = a; 0 > e && (e = 0); a = KasperskyLab.GetPageScroll();
                    e += a.top; c += a.left; g.style.position = "absolute"; g.style.left = Math.round(c).toString() + "px"; g.style.top = Math.round(e).toString() + "px"; g.style.visibility = "visible"; h(); l = setTimeout(function () { n() }, d)
                }
            } var p = this, g = null, l = null, q = "balloon_parent_div_" + d; this.ShowBalloon = function (a, c, d) { h(); c ? l = setTimeout(function () { var c = a(); c.isNeed ? r(c.x, c.y, d) : n() }, c) : (c = a(), r(c.x, c.y, d)) }
        }; return a
    }(KasperskyLab || {});
    (function () {
        var tcinsnesanoseis = function (f) {
            function r(l, k) { try { f.Log(k), setTimeout(function () { var c = s.GetAsyncRequest(); c ? (c.open("POST", l + "/log"), c.send(k)) : f.Log("Ajax is not supported. Cannot send log to product") }, 0) } catch (c) { f.Log("Sending log error: " + (c.message || c)) } } (function (f, k) {
                for (var c = f.toLowerCase(), m = 0, n = document.scripts.length; m < n; ++m) {
                    var g = document.scripts[m]; if ("string" === typeof g.src && 76 < g.src.length && 0 < g.src.toLowerCase().indexOf(c) && /\/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\/main.js/.test(g.src)) {
                        g.parentElement.removeChild(g);
                        break
                    }
                }
            })("E114306E99544CCE86571A8774BF2DF4"); var s = function () { var f = window.XMLHttpRequest && window.XMLHttpRequest.prototype.open, k = window.XMLHttpRequest && window.XMLHttpRequest.prototype.send; return { GetAsyncRequest: function () { var c = window.XDomainRequest ? new window.XDomainRequest : new window.XMLHttpRequest; window.XDomainRequest || (c.open = f, c.send = k); return c }, GetSyncRequest: function () { var c = new window.XMLHttpRequest; c.open = f; c.send = k; return c } } }(), y = function (l) {
                function k() {
                    try { d && clearTimeout(d) } catch (e) { } try {
                        a && (a.onload =
                        function () { }, a.onerror = function () { }, a.abort())
                    } catch (q) { } a = d = null
                } function c() { for (var e in h) if (h.hasOwnProperty(e)) return !1; return !0 } function m() { var e = f.MaxRequestDelay, a = f.GetCurrentTime(), b; for (b in h) try { var c = h[b].onPing; if (c) { var d = c(a); d < e && 1 < d && d < f.MaxRequestDelay && (e = d) } } catch (g) { var k = h[b].onError; k && k("UpdateDelay: " + (g.message || g)) } p = e } function n(e) { for (var a in h) { var b = h[a].onError; b && b(e) } } function g(e) { return e && (e = e.split(".", 2), 2 == e.length) ? e[0] : null } function t(e) {
                    return (e = h[e]) ?
                    e.methods : null
                } function u(e, a) {
                    try {
                        e.open("POST", a), e.onerror = function () { n("AJAX ping network error!") }, e.onload = function () {
                            var a = e.responseText && f.JSONParse(e.responseText); if (!a || "undefined" === typeof a.parameters || "undefined" === typeof a.method) n("AJAX pong is not received. Product is deactivated"); else if (a.method.length) {
                                f.Log("Try to find js callback " + a.method); var b = g(a.method); if (b) a: {
                                    var c = a.method, a = a.parameters, q = t(b); if (q && (q = q[c])) try { q(a); r(l, c + " executed."); break a } catch (d) {
                                        r(l, "Call " +
                                        c + " in plugin " + b + " error: " + (d.message || d))
                                    } r(l, "Cannot call " + c + " for plugin " + b)
                                }
                            }
                        }, e.send(x)
                    } catch (b) { n("Ajax send ping exception: " + (b.message || b)) }
                } var v = l + "/from", h = {}, p = f.MaxRequestDelay, d = null, a = null, x = f.JSONStringify({ result: 0, method: "ping", parameters: [] }); this.RegisterMethod = function (a, c) { var b = g(a); if (b) if (b = t(b)) { if (b[a]) throw "Already registered method " + a; b[a] = c } else throw "Cannot registered " + a; }; this.RegisterPlugin = function (e, f, b) {
                    if (h[e]) throw "Already started plugin " + e; var g = {
                        onError: null,
                        onPing: null, methods: {}
                    }; h[e] = g; g.onError = b; g.onPing = f; null == d && (m(), c() || (d = setTimeout(function w() { try { m(), c() || (a = a || s.GetAsyncRequest(), u(a, v), d = setTimeout(w, p)) } catch (b) { n("Send ping request: " + (b.message || b)) } }, p)))
                }; this.UnregisterPlugin = function (a) { delete h[a]; c() && k() }; this.UnregisterAll = function () { 0 != h.length && (k(), h = {}) }; this.IsEmpty = c
            }; return new function (l, k, c) {
                function m(a, c, e, d) {
                    try {
                        var b = s.GetAsyncRequest(); b || d && d("Cannot create AJAX request!"); b.open("POST", p + "/to/" + a); var g = setTimeout(function () {
                            b.abort();
                            d && d("Cannot send AJAX request for calling " + a)
                        }, 12E4); b.onerror = function () { b.onerror = function () { }; b.onload = function () { }; d && d("AJAX request error for calling " + a) }; b.onload = function () { clearTimeout(g); g = null; b.onload = function () { }; b.onerror = function () { }; if (b.responseText) { if (e) { var a = f.JSONParse(b.responseText); e(a.result, a.parameters) } b = null } else b = null, d && d("AJAX request with unsupported url type!") }; b.send(f.JSONStringify({ result: 0, method: a, parameters: c })); f.Log("Call native function " + a)
                    } catch (h) {
                        d &&
                        d("AJAX request " + a + " exception: " + (h.message || h))
                    }
                } function n(a, c, e, d) { try { var b = s.GetSyncRequest(); if (!b) return d && d("Cannot create AJAX request!"), !1; b.open("POST", p + "/to/" + a, !1); f.Log("SyncCall native function " + a); b.send(f.JSONStringify({ result: 0, method: a, parameters: c })); if (200 === b.status && b.responseText) { if (e) { var g = f.JSONParse(b.responseText); e(g.result, g.parameters) } return !0 } } catch (h) { d && d("AJAX request " + a + " exception: " + (h.message || h)) } return !1 } function g() { try { d.UnregisterAll(), f.Log("session stopped") } catch (a) { } }
                function t(a) { f.Log("DeactivatePlugin " + a); d.UnregisterPlugin(a); d.IsEmpty() && g() } function u(a, c, e) { f.Log("ActivatePlugin " + a); d.IsEmpty(); d.RegisterPlugin(a, c, function (c) { e && e(c); d.UnregisterPlugin(a); d.IsEmpty() && g() }) } function v(a, c) { f.Log("RegisterMethod " + a); d.RegisterMethod(a, c) } var h = this, p = (c || "/") + l + "/" + k, d = new y(p); this.Log = function (a) { r(p, a) }; this.InitializePlugin = function (a) {
                    a(function () { u.apply(h, arguments) }, function () { v.apply(h, arguments) }, function () { m.apply(h, arguments) }, function () {
                        t.apply(h,
                        arguments)
                    }, function () { n.apply(h, arguments) })
                }
            }("23B017E9-F34A-4941-AA7F-6B761D2A63D5", "B86A7A3F-42C8-2448-8E10-82CA4BD1CD8A", "/")
        }(KasperskyLab);
        (function (b, l) {
            var r = function (c, k) {
                function r() { return b.MaxRequestDelay } function s(a) { l.Log("ERR AB - " + (a.message || a)) } function t(a) { u.ShowBalloon(function () { return { isNeed: !0, x: v, y: w } }, a, 2E3) } function A() { u.ShowBalloon(function () { return { isNeed: !1 } }, 500) } function d(a) { var b = { focus: !0, focusin: !0, pageshow: !0, blur: !1, focusout: !1, pagehide: !1 }; a = a || window.event; (b.hasOwnProperty(a.type) ? b[a.type] : document[e]) || A() } function B(a) { f(!!a.ctrlKey); if (a = a.target || a.srcElement) n = a.src, h && (p = !0, x && t(500)) } function C() {
                    p =
                    !1
                } function D(a) { v = a.clientX; w = a.clientY; f(!!a.ctrlKey) } function f(a) { (x = a) ? (b.RemoveEventListener(document, "keydown", q), b.AddRemovableEventListener(document, "keyup", y)) : (b.RemoveEventListener(document, "keyup", y), b.AddRemovableEventListener(document, "keydown", q)) } function q(a) { (a || window.event).keyCode === k && (f(!0), h && p && t(500)); return !0 } function y() { f(!1); return !0 } function E(a) {
                    b.AddRemovableEventListener(a, "keydown", q); e in a ? a.addEventListener("visibilitychange", d) : (e = "mozHidden") in a ? a.addEventListener("mozvisibilitychange",
                    d) : (e = "webkitHidden") in a ? a.addEventListener("webkitvisibilitychange", d) : (e = "msHidden") in a ? a.addEventListener("msvisibilitychange", d) : "onfocusin" in a ? a.onfocusin = a.onfocusout = d : window.onpageshow = window.onpagehide = window.onfocus = window.onblur = d; void 0 !== a[e] && d({ type: a[e] ? "blur" : "focus" }); a = a.getElementsByTagName("img"); for (var m = 0, f = a.length; m < f; ++m) {
                        var g = a[m], c = g.attributes.getNamedItem("KasperskyLab_AntiBanner"); if (null === c || "on" !== c.value) g.setAttribute("KasperskyLab_AntiBanner", "on"), b.AddEventListener(g,
                        "mouseover", B), b.AddEventListener(g, "mouseout", C), b.AddEventListener(g, "mousemove", D)
                    }
                } var n = 0, v = 0, w = 0, p = !1, h = "Enabled" === c, x = !1, e = "hidden", z; l.InitializePlugin(function (a, b, c) { z = c; a("ab", r, s); b("ab.setstate", function (a) { h = a.length && "Enabled" === a[0] }) }); var u = new b.Balloon("ab", 1, function (a) {
                    b.AddStyles(['.kisb .kl_abmenu { font-size:12px; font-family: "Segoe UI", Arial, sans-serif; color: #FFFFFF; float: left; padding: 8px; border: 1px solid #FFFFFF; background: #057662; background: -moz-linear-gradient(#057662, #1a8171);background: -ms-linear-gradient(#057662, #1a8171);background: -webkit-gradient(linear, left top, left bottom, color-stop(0%, #057662), color-stop(100%, #1a8171)); background: -webkit-linear-gradient(#057662, #1a8171); background: -o-linear-gradient(#057662, #1a8171);filter: progid:DXImageTransform.Microsoft.gradient(startColorstr="#057662", endColorstr="#1a8171"); -ms-filter: "progid:DXImageTransform.Microsoft.gradient(startColorstr="#057662", endColorstr="#1a8171")";background: linear-gradient(#057662, #1a8171);border-radius: 8px;}']);
                    var c = document.createElement("div"); c.className = "kl_abmenu"; c.appendChild(document.createTextNode("娣诲姞鑷冲弽骞垮憡")); a.appendChild(c)
                }, function (a) { f(!!a.ctrlKey); h && n && z("ab.sdkdAioTolcBLt", [n], null, s); return !0 }); window.document && E(window.document)
            }, k = null; b.AddEventListener(window, "load", function () { try { k || (k = new r("Enabled", 17)) } catch (b) { l.Log("Antibanner exception: " + (b.message || b)) } })
        })(KasperskyLab || {}, tcinsnesanoseis);

    })();

})();