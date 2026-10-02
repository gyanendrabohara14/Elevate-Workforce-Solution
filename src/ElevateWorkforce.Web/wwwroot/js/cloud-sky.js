(function () {
    "use strict";

    var vertexSource = "attribute vec2 a_pos; void main(){ gl_Position=vec4(a_pos,0.0,1.0); }";
    var fragmentSource = `
        precision highp float;
        uniform vec2 uRes;
        uniform float uNearX, uFarX, uCirrusX, uCoverage, uSize, uSoftness, uShadow, uCirrus;
        uniform vec3 uZenith, uHorizon, uCloud;
        uniform vec4 uGlow;
        uniform vec2 uSun, uParallax;

        vec2 hash22(vec2 p){
            vec3 q=fract(vec3(p.xyx)*vec3(0.1031,0.1030,0.0973));
            q+=dot(q,q.yzx+33.33);
            return fract((q.xx+q.yz)*q.zy);
        }
        float hash12(vec2 p){
            vec3 q=fract(vec3(p.xyx)*0.1031);
            q+=dot(q,q.yzx+33.33);
            return fract((q.x+q.y)*q.z);
        }
        float vnoise(vec2 x){
            vec2 i=floor(x), f=fract(x);
            f=f*f*(3.0-2.0*f);
            return mix(mix(hash12(i),hash12(i+vec2(1.0,0.0)),f.x),
                       mix(hash12(i+vec2(0.0,1.0)),hash12(i+vec2(1.0,1.0)),f.x),f.y);
        }
        float fbm(vec2 p){
            float a=0.5, sum=0.0;
            for(int i=0;i<4;i++){ sum+=a*vnoise(p); p*=2.03; a*=0.5; }
            return sum;
        }
        vec2 blobs(vec2 uv,float seed){
            vec2 id=floor(uv), f=fract(uv);
            float best=-1e4, weight=0.0, ysum=0.0;
            float wmax=min(2.15,0.72*uSize);
            float reach=min(2.0,ceil(wmax+0.85)-1.0);
            for(int j=-2;j<=2;j++) for(int i=-2;i<=2;i++){
                vec2 offset=vec2(float(i),float(j));
                if(max(abs(offset.x),abs(offset.y))>reach) continue;
                vec2 h=hash22(id+offset+seed);
                if(fract(h.x*37.1)>uCoverage) continue;
                vec2 center=offset+0.15+h*0.7;
                float w=min(2.15,(0.30+0.42*fract(h.y*19.7))*uSize);
                vec2 d=f-center;
                float ry=(d.y>0.0?0.34:0.19)*uSize*(0.8+0.5*fract(h.y*7.3));
                float edge=1.0-length(vec2(d.x/max(w,1e-3),d.y/max(ry,1e-3)));
                float yn=d.y/max(ry,1e-3);
                if(edge>best){
                    float k=exp(12.0*(best-edge));
                    weight=weight*k+1.0; ysum=ysum*k+yn; best=edge;
                }else{
                    float k=exp(12.0*(edge-best));
                    weight+=k; ysum+=k*yn;
                }
            }
            return vec2(best,ysum/max(weight,1e-4));
        }
        vec2 cloudField(vec2 uv,float seed,float detailScale){
            vec2 shape=blobs(uv,seed);
            float detail=fbm(uv*detailScale+seed*3.1)*0.72
                       +fbm(uv*detailScale*3.3+seed*7.7)*0.28;
            return vec2(shape.x-(1.0-detail)*0.7,shape.y);
        }
        vec3 shadeCloud(float dy,vec3 sky){
            float top=smoothstep(-0.95,0.25,dy);
            vec3 base=mix(uCloud*0.52,sky,0.34);
            return mix(mix(uCloud,base,uShadow),uCloud,top);
        }
        void main(){
            float aspect=uRes.x/max(uRes.y,1.0);
            vec2 p=gl_FragCoord.xy/max(uRes.y,1.0);
            vec3 sky=mix(uHorizon,uZenith,smoothstep(-0.15,1.05,p.y));
            vec2 sunPosition=vec2(uSun.x*aspect,uSun.y);
            sky+=uGlow.rgb*uGlow.a*exp(-length(p-sunPosition)*3.4)*0.30;
            vec3 color=sky;
            if(uCirrus>0.0){
                vec2 uv=vec2(p.x*1.4+uCirrusX,p.y*5.5);
                float veil=fbm(uv)*fbm(uv*2.3+9.0);
                veil=smoothstep(0.24,0.55,veil)*smoothstep(0.15,0.7,p.y);
                color=mix(color,uCloud,veil*uCirrus*0.5);
            }
            vec2 farUv=(vec2(p.x+uFarX,p.y)*2.15)+uParallax*0.4;
            vec2 farCloud=cloudField(farUv,17.0,11.0);
            float farAlpha=clamp(farCloud.x*uSoftness,0.0,1.0);
            vec3 farLit=shadeCloud(farCloud.y,sky);
            color=mix(color,mix(farLit,sky,0.55),farAlpha);
            vec2 nearUv=vec2(p.x+uNearX,p.y)*1.05+uParallax;
            vec2 nearCloud=cloudField(nearUv,3.0,8.5);
            float nearAlpha=clamp(nearCloud.x*uSoftness,0.0,1.0);
            vec3 nearLit=shadeCloud(nearCloud.y,sky);
            float above=clamp(cloudField(nearUv+vec2(0.0,0.085),3.0,8.5).x*uSoftness,0.0,1.0);
            nearLit*=1.0-0.18*uShadow*above;
            nearLit+=uGlow.rgb*uGlow.a*0.22*exp(-length(p-sunPosition)*1.6);
            color=mix(color,nearLit,nearAlpha);
            gl_FragColor=vec4(color,1.0);
        }
    `;

    function compileShader(gl, type, source) {
        var shader = gl.createShader(type);
        if (!shader) return null;
        gl.shaderSource(shader, source);
        gl.compileShader(shader);
        if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
            console.warn("Cloud sky shader unavailable:", gl.getShaderInfoLog(shader));
            gl.deleteShader(shader);
            return null;
        }
        return shader;
    }

    function makeCloudSky(section) {
        var canvas = document.createElement("canvas");
        canvas.className = "sky-canvas";
        canvas.setAttribute("aria-hidden", "true");
        section.insertBefore(canvas, section.firstChild);

        var gl = canvas.getContext("webgl", { alpha: false, antialias: false, depth: false, powerPreference: "low-power" });
        if (!gl) return;

        var vertex = compileShader(gl, gl.VERTEX_SHADER, vertexSource);
        var fragment = compileShader(gl, gl.FRAGMENT_SHADER, fragmentSource);
        if (!vertex || !fragment) return;

        var program = gl.createProgram();
        if (!program) return;
        gl.attachShader(program, vertex);
        gl.attachShader(program, fragment);
        gl.linkProgram(program);
        if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
            console.warn("Cloud sky program unavailable:", gl.getProgramInfoLog(program));
            return;
        }
        gl.useProgram(program);

        var buffer = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 3, -1, -1, 3]), gl.STATIC_DRAW);
        var position = gl.getAttribLocation(program, "a_pos");
        gl.enableVertexAttribArray(position);
        gl.vertexAttribPointer(position, 2, gl.FLOAT, false, 0, 0);

        var uniforms = {};
        function uniform(name) {
            if (!(name in uniforms)) uniforms[name] = gl.getUniformLocation(program, name);
            return uniforms[name];
        }

        var reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        var pointer = { x: 0, y: 0, inside: false };
        var leanX = 0;
        var leanY = 0;
        var animationFrame = 0;
        var visible = true;
        var lastTime = 0;
        var dpr = Math.min(window.devicePixelRatio || 1, 1.5);

        function draw(now) {
            animationFrame = 0;
            var rect = section.getBoundingClientRect();
            var width = Math.max(1, Math.round(rect.width * dpr));
            var height = Math.max(1, Math.round(rect.height * dpr));
            if (canvas.width !== width || canvas.height !== height) {
                canvas.width = width;
                canvas.height = height;
            }
            gl.viewport(0, 0, width, height);

            var dt = lastTime ? Math.min(0.05, (now - lastTime) / 1000) : 0;
            lastTime = now;
            var damping = 1 - Math.exp(-6 * dt);
            leanX += ((pointer.inside ? pointer.x : 0) - leanX) * damping;
            leanY += ((pointer.inside ? pointer.y : 0) - leanY) * damping;

            gl.uniform2f(uniform("uRes"), width, height);
            gl.uniform1f(uniform("uNearX"), -now * 0.00007);
            gl.uniform1f(uniform("uFarX"), -now * 0.000033);
            gl.uniform1f(uniform("uCirrusX"), -now * 0.000018);
            gl.uniform1f(uniform("uCoverage"), 0.88);
            gl.uniform1f(uniform("uSize"), 1.3);
            gl.uniform1f(uniform("uSoftness"), 4.5 / 2.0);
            gl.uniform1f(uniform("uShadow"), 0.7);
            gl.uniform1f(uniform("uCirrus"), 1.0);
            gl.uniform2f(uniform("uSun"), 1.0, 1.0);
            gl.uniform2f(uniform("uParallax"), -leanX * 0.21, -leanY * 0.15);
            gl.uniform3f(uniform("uZenith"), 0.0, 0.46, 1.0);
            gl.uniform3f(uniform("uHorizon"), 0.71, 0.82, 0.94);
            gl.uniform3f(uniform("uCloud"), 1.0, 1.0, 1.0);
            gl.uniform4f(uniform("uGlow"), 1.0, 1.0, 1.0, 0.9);
            gl.drawArrays(gl.TRIANGLES, 0, 3);

            if (!reducedMotion && visible && !document.hidden) {
                animationFrame = window.requestAnimationFrame(draw);
            }
        }

        function scheduleDraw() {
            if (!animationFrame && visible && !document.hidden) {
                animationFrame = window.requestAnimationFrame(draw);
            }
        }

        function onPointerMove(event) {
            var rect = section.getBoundingClientRect();
            pointer.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
            pointer.y = 1 - ((event.clientY - rect.top) / rect.height) * 2;
            pointer.inside = true;
            scheduleDraw();
        }

        function onPointerLeave() {
            pointer.inside = false;
            scheduleDraw();
        }

        function onVisibilityChange() {
            if (document.hidden) {
                if (animationFrame) window.cancelAnimationFrame(animationFrame);
                animationFrame = 0;
            } else {
                lastTime = 0;
                scheduleDraw();
            }
        }

        section.addEventListener("pointermove", onPointerMove, { passive: true });
        section.addEventListener("pointerleave", onPointerLeave, { passive: true });
        document.addEventListener("visibilitychange", onVisibilityChange);

        var observer = "IntersectionObserver" in window ? new IntersectionObserver(function (entries) {
            visible = entries.some(function (entry) { return entry.isIntersecting; });
            if (visible) scheduleDraw();
            else if (animationFrame) {
                window.cancelAnimationFrame(animationFrame);
                animationFrame = 0;
            }
        }, { rootMargin: "80px" }) : null;

        if (observer) observer.observe(section);
        draw(performance.now());
    }

    document.querySelectorAll(".hero, .jobs-hero, .page-hero, .auth-wrap").forEach(makeCloudSky);
})();
