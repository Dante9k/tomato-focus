"""Deterministic 36-second portrait product film. No network or desktop capture.

Requires Pillow, numpy and imageio-ffmpeg. Run from any directory:
  python marketing/xiaohongshu/render.py
  python marketing/xiaohongshu/render.py --stills
Outputs are isolated under dist/promo-zhuguo; existing app/site files are untouched.
"""
from pathlib import Path
from functools import lru_cache
import argparse
import json
import math
import shutil
import subprocess
import wave

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "dist/promo-zhuguo"
W, H, FPS, DURATION = 1080, 1920, 30, 36
CREAM, GREEN, RED = "#F6F3EB", "#203C31", "#E7583F"
MUTED, PALE = "#6C7D71", "#E3E8D9"
ART = Image.open(ROOT / "assets/tomato-cute.png").convert("RGBA")
SETTINGS = Image.open(ROOT / "docs/images/settings-panel.png").convert("RGBA")
STARTS = [0, 3.5, 6.5, 11.5, 15.5, 19.5, 24.5, 28, 31.5, 36]
CAPTIONS = [
    "认真专注，也可以有一点可爱。",
    "朱果，一颗住在 Windows 桌面上的番茄钟。",
    "滚动数字，设置小时、分钟和秒。",
    "调好时间，双击绿蒂，开始专注。",
    "自动缩小、变淡，保持置顶。",
    "时间到，小番茄从绿蒂持续抛出。",
    "拖动大番茄停止；双击或 Esc 也可以。",
    "右键打开设置，选择 25 / 5 / 15 分钟。",
    "关注我，获取朱果上线动态。",
]


def ease(v):
    v = max(0.0, min(1.0, v))
    return v * v * (3 - 2 * v)


def mix(a, b, v):
    return a + (b - a) * v


@lru_cache(maxsize=120)
def font(size, bold=False, latin=False):
    name = ("segoeuib.ttf" if bold else "segoeui.ttf") if latin else ("msyhbd.ttc" if bold else "msyh.ttc")
    return ImageFont.truetype("C:/Windows/Fonts/" + name, size)


def text(im, value, x, y, size=36, color=GREEN, bold=False, anchor="lt", latin=False):
    ImageDraw.Draw(im).text((round(x), round(y)), value, font=font(size, bold, latin), fill=color, anchor=anchor)


def rr(im, box, fill, radius=28, outline=None, width=1):
    ImageDraw.Draw(im).rounded_rectangle(tuple(round(v) for v in box), radius, fill=fill, outline=outline, width=width)


def pill(im, value, x, y, fill=PALE, color=GREEN, size=27):
    tw = ImageDraw.Draw(im).textlength(value, font=font(size))
    rr(im, (x, y, x + tw + 40, y + size + 29), fill, 24)
    text(im, value, x + 20, y + 12, size, color)


@lru_cache(maxsize=300)
def fruit(size):
    return ART.resize((size, size), Image.Resampling.LANCZOS)


def paste(im, obj, x, y, opacity=1):
    if opacity < .999:
        obj = obj.copy()
        obj.putalpha(obj.getchannel("A").point(lambda a: round(a * opacity)))
    im.paste(obj, (round(x), round(y)), obj)


def tomato(im, x, y, size, rotation=0, opacity=1):
    obj = fruit(max(1, round(size)))
    if abs(rotation) > .5:
        obj = obj.rotate(rotation, resample=Image.Resampling.BICUBIC, expand=True)
    paste(im, obj, x - obj.width / 2, y - obj.height / 2, opacity)


@lru_cache(maxsize=180)
def widget_image(size, mode="edit", minutes=25, second=0):
    # Coordinates follow TomatoWindow.cs; this enlarged reconstruction is labelled as animation.
    size = int(size)
    im = fruit(size).copy()
    q = size / 250
    def tx(s, x, y, fs, color, bold=False):
        text(im, s, x*q, y*q, max(1, round(fs*q)), color, bold, anchor="mt", latin=True)
    if mode == "edit":
        overlay = Image.new("RGBA", (size, size))
        rr(overlay, (38*q, 100*q, 212*q, 192*q), (70, 16, 18, 170), round(19*q))
        rr(overlay, (45*q, 130*q, 203*q, 160*q), (255, 225, 199, 28), round(10*q))
        im = Image.alpha_composite(im, overlay)
        for col, val, count in [(77, 0, 24), (125, minutes, 60), (173, 0, 60)]:
            tx(f"{(val-1)%count:02d}", col, 106, 19, "#D78772")
            tx(f"{val:02d}", col, 131, 24, "#FFF2E3")
            tx(f"{(val+1)%count:02d}", col, 162, 19, "#D78772")
        tx(":", 101, 133, 20, "#EFBD9B")
        tx(":", 149, 133, 20, "#EFBD9B")
        tx("···", 211, 86, 17, "#FFF2E3", True)
    elif mode == "focus":
        # The app uses custom recessed paths; light and inner edge approximate them in this film.
        tx(f"{minutes:02d}:{second:02d}", 125, 132, 32, "#943A2C", True)
        tx(f"{minutes:02d}:{second:02d}", 125, 133.3, 32, "#FFF4E9", True)
    elif mode == "alarm":
        text(im, "时间到了", 125*q, 141*q, round(25*q), "#FFF4DE", True, anchor="mt")
        text(im, "休息一下", 125*q, 176*q, round(11*q), "#FFDBBC", anchor="mt")
    return im


def widget(im, cx, cy, size, mode="edit", minutes=25, second=0, opacity=1):
    obj = widget_image(round(size), mode, minutes, second)
    paste(im, obj, cx - size/2, cy - size/2, opacity)


def cursor(im, x, y, click=-1, scale=1):
    d = ImageDraw.Draw(im)
    if 0 <= click <= .5:
        radius = (18 + click*100) * scale
        d.ellipse((x-radius,y-radius,x+radius,y+radius), outline=RED, width=3)
    points = [(0,0),(3,54),(16,41),(28,66),(41,59),(27,35),(46,33)]
    pts = [(x+a*scale, y+b*scale) for a,b in points]
    d.polygon(pts, fill="white", outline=GREEN, width=3)


@lru_cache(maxsize=3)
def backdrop(dark=False):
    color = GREEN if dark else CREAM
    im = Image.new("RGB", (W,H), color)
    d = ImageDraw.Draw(im)
    ring = "#29483B" if dark else "#E7E8DC"
    for r in (280, 420, 570):
        d.ellipse((540-r,1010-r,540+r,1010+r), outline=ring, width=2)
    return im


def chrome(im, t, idx, dark=False):
    fg, mute = (CREAM, "#B3C1AD") if dark else (GREEN,MUTED)
    tomato(im, 119, 142, 62)
    text(im, "朱果", 164, 115, 38, fg, True)
    text(im, "TOMATO FOCUS", 166, 166, 19, mute, latin=True)
    text(im, "桌面专注小工具", 926, 136, 24, mute, anchor="rt")
    text(im, "功能演示动画", 100, 1745, 22, mute)
    text(im, "Windows 10 / 11", 934, 1745, 22, mute, anchor="rt", latin=True)
    for i in range(9):
        x = 100+i*94
        rr(im,(x,1692,x+78,1697), "#496352" if dark else "#DBDFD1", 2)
        v = max(0,min(1,(t-STARTS[i])/(STARTS[i+1]-STARTS[i])))
        if v>0:
            rr(im,(x,1692,x+78*v,1697), "#F3A186" if dark else RED,2)
    # Text stays above bottom overlay region; all key content is within x=100..934.
    rr(im,(86,1534,960,1636),"#2D4C3E" if dark else "#E9EBDD",28)
    text(im,CAPTIONS[idx],523,1568,30,fg,anchor="mt")


def heading(im, kicker, lines, local, dark=False):
    fg = CREAM if dark else GREEN
    y = 290+round((1-ease(local/.45))*26)
    text(im, kicker,100,y,25,"#F5AF95" if dark else RED,True)
    for i,line in enumerate(lines):
        text(im,line,96,y+66+i*101,80,fg,True)


def desk(im, progress=.1):
    rr(im,(90,687,952,1398),"#E4E9DF",38)
    rr(im,(122,758,915,1315),"#FDFCF7",24)
    rr(im,(122,758,915,817),"#EDF0E6",24)
    d = ImageDraw.Draw(im)
    for x,c in [(150,"#D8816B"),(174,"#D7BE7B"),(198,"#A9BD96")]:
        d.ellipse((x,781,x+12,793),fill=c)
    text(im,"今天，只做眼前这一件。",172,868,38,GREEN,True)
    text(im,"我的专注清单",173,949,24,MUTED)
    for i,label in enumerate(["写好开头","补充关键内容","完成第一版"]):
        y=1020+i*74
        rr(im,(174,y,202,y+28),"#FDFCF7",7,outline="#A3B19C",width=2)
        text(im,label,223,y-1,28,GREEN)
        if progress > (i+1)/3:
            d.line((180,y+13,187,y+21,199,y+6),fill=RED,width=4)
    text(im,"专注中",806,1350,24,MUTED)
    rr(im,(139,1355,690,1363),"#CAD3C1",4)
    rr(im,(139,1355,139+551*progress,1363),"#86A07E",4)


def particle_position(age, n, origin=(800,820), ground=1390):
    vx = -185 - (n*71 % 230)
    vy = -490 - (n*83 % 170)
    gravity = 870
    x0,y0 = origin
    hit = (-vy + math.sqrt(vy*vy+2*gravity*(ground-y0)))/gravity
    x = x0+vx*age
    if age <= hit:
        y = y0+vy*age+.5*gravity*age*age
    else:
        a=age-hit
        velocity=(vy+gravity*hit)*.42
        bounce=2*velocity/gravity
        if a < bounce:
            y=ground-velocity*a+.5*gravity*a*a
        else:
            y=ground
    return x,y,hit


def rain(im, local, origin=(800,820), interval=.29, count=22, ground=1390):
    for n in range(count):
        age=local-n*interval
        if age<0 or age>3.7: continue
        x,y,_=particle_position(age,n,origin,ground)
        size=66+(n*13%43)
        if -120<x<1180:
            tomato(im,x,y,size,rotation=age*(140 if n%2 else -155))


def scene(idx, local, global_t):
    dark=idx in (5,8)
    im=backdrop(dark).copy()
    if idx==0:
        heading(im,"给桌面一点可爱",["我的桌面，","会下番茄雨。"],local)
        widget(im,650,970+8*math.sin(local*2),500,"alarm")
        rain(im,local+.65,(650,808),.28,16,1400)
        pill(im,"到点提醒，也太可爱了",100,626)
    elif idx==1:
        heading(im,"认识一下 / MEET ZHUGUO",["朱果","住在桌面的小番茄"],local)
        widget(im,540,1032+12*math.sin(local*2),620)
        pill(im,"透明桌面",125,1365)
        pill(im,"无需登录",393,1365)
        pill(im,"离线可用",661,1365)
    elif idx==2:
        heading(im,"01 / 设定时间",["滚一滚，","留出 25 分钟。"],local)
        minute=20+min(5,int(max(0,local-.7)*2.4))
        widget(im,540,1010,680,"edit",minute)
        if local>.45:
            cy=1010+((local*2.4)%1-.5)*45
            cursor(im,564,cy,scale=1.15)
            d=ImageDraw.Draw(im)
            d.line((785,955,785,1060),fill=RED,width=5)
            d.line((773,972,785,953,797,972),fill=RED,width=5)
            d.line((773,1043,785,1062,797,1043),fill=RED,width=5)
        for x,s in [(409,"小时"),(540,"分钟"),(671,"秒")]:
            text(im,s,x,1330,28,MUTED,anchor="mt")
        text(im,"滚轮 / 上下拖动 / 直接输入",540,1424,26,MUTED,anchor="mt")
    elif idx==3:
        heading(im,"02 / 开始专注",["双击绿蒂，","把时间交给朱果。"],local)
        shrink=ease((local-1.9)/.46)
        desk(im,.12)
        # Keep the top-right anchor fixed while shrinking, matching the app.
        size=mix(520,260,shrink)
        cx=820-size/2
        cy=742+size/2
        widget(im,cx,cy,size,"edit" if shrink<.01 else "focus",25,0,1-.68*shrink)
        if local<2.35:
            click=min(abs(local-1.45),abs(local-1.68)) if local>=1.45 else -1
            cursor(im,570,830,click,1.05)
        if local<1.9:
            pill(im,"点这里 × 2",325,670,fill=GREEN,color=CREAM)
        else:
            pill(im,"缩小 · 变淡 · 保持置顶",214,1430)
    elif idx==4:
        heading(im,"给工作留出位置",["它轻轻陪着，","你慢慢完成。"],local)
        desk(im,.18+local*.2)
        widget(im,690,872,260,"focus",24,max(0,59-int(local)),.32)
        pill(im,"专注时自动缩小、变淡",217,1430)
    elif idx==5:
        heading(im,"专注结束 / TAKE A BREAK",["时间到。","让番茄飞一会儿。"],local,True)
        p=ease(local/.46)
        widget(im,690-mix(0,120,p),872+mix(0,135,p),mix(260,500,p),"alarm",opacity=.32+.68*p)
        rain(im,max(0,local-.5),(570,839),.27,18,1410)
        pill(im,"倒计时已快进",100,622,fill="#355443",color="#D6DFC9",size=24)
        text(im,"投出、落地，配上轻巧音效。",100,1454,30,"#D6DFC9")
    elif idx==6:
        heading(im,"03 / 结束提醒",["轻轻一拖，","收好这场番茄雨。"],local)
        move=ease((local-1)/.8)
        cx,cy=570-170*move,1007+45*move
        widget(im,cx,cy,500,"alarm" if local<1.05 else "edit")
        if local<1.05:
            rain(im,local+2,(570,839),.27,18,1410)
        cursor(im,cx+30,cy+50,scale=1.1)
        if local>1.9: pill(im,"提醒停止，恢复时间设置",220,1405)
    elif idx==7:
        heading(im,"还有，适合你的节奏",["专注与休息，","都安排得刚刚好。"],local)
        panel=SETTINGS.resize((436,716),Image.Resampling.LANCZOS)
        paste(im,panel,108,684)
        text(im,"25 分钟",618,804,48,GREEN,True)
        text(im,"先专注一件事",620,875,28,MUTED)
        text(im,"5 / 15 分钟",618,986,40,GREEN,True)
        text(im,"给休息留一点时间",620,1055,26,MUTED)
        text(im,"声音，自己决定",618,1207,29,RED,True)
        text(im,"预设需手动选择",618,1264,25,MUTED)
        text(im,"右键番茄，打开设置",100,1431,30,MUTED)
    else:
        heading(im,"一颗番茄，一段完整的专注",["把时间，","留给喜欢的事。"],local,True)
        widget(im,540,1005+10*math.sin(local*1.7),580)
        text(im,"朱果",540,1302,60,CREAM,True,anchor="mt")
        text(im,"TOMATO FOCUS",540,1386,27,"#BDCCB5",anchor="mt",latin=True)
        text(im,"Windows 桌面番茄钟",540,1445,29,"#BDCCB5",anchor="mt")
    chrome(im,global_t,idx,dark)
    return im


def frame(t):
    idx=next((i for i in range(9) if t<STARTS[i+1]),8)
    local=t-STARTS[idx]
    im=scene(idx,local,t)
    # Short dissolves; the hook is visible immediately at frame zero.
    if idx>0 and local<.2:
        previous=scene(idx-1,STARTS[idx]-STARTS[idx-1]+local,t)
        im=Image.blend(previous,im,ease(local/.2))
    return im


def cover():
    im=Image.new("RGB",(1080,1440),CREAM)
    d=ImageDraw.Draw(im)
    d.ellipse((113,455,973,1315),fill="#E6EAD9")
    tomato(im,127,104,68)
    text(im,"朱果 / TOMATO FOCUS",177,88,28,GREEN,True)
    text(im,"我的桌面",91,216,100,GREEN,True)
    text(im,"会下番茄雨",91,348,100,RED,True)
    pill(im,"一颗会提醒你休息的番茄钟",99,510,fill=GREEN,color=CREAM,size=29)
    widget(im,567,947,660)
    for x,y,s,a in [(167,799,130,-25),(913,715,138,26),(160,1160,98,33),(886,1192,117,-22)]:
        tomato(im,x,y,s,a)
    text(im,"调时间 → 双击开始 → 到点休息",540,1280,34,GREEN,True,anchor="mt")
    text(im,"Windows 10 / 11  ·  无需登录  ·  离线可用",540,1358,26,MUTED,anchor="mt")
    im.save(OUT/"zhuguo-cover-3x4.jpg",quality=96,subsampling=0)


def audio():
    rate=44100
    sound=np.zeros((DURATION*rate,2),dtype=np.float32)
    rng=np.random.default_rng(20260922)
    def add(at,signal,gain=1,pan=0):
        start=round(at*rate)
        if start<0 or start>=len(sound): return
        signal=np.asarray(signal,dtype=np.float32)[:len(sound)-start]
        sound[start:start+len(signal),0]+=signal*gain*math.sqrt((1-pan)/2)
        sound[start:start+len(signal),1]+=signal*gain*math.sqrt((1+pan)/2)
    def note(midi,length=.75):
        tm=np.arange(round(length*rate))/rate
        hz=440*2**((midi-69)/12)
        env=(1-np.exp(-tm*170))*np.exp(-tm*6)
        return (np.sin(2*np.pi*hz*tm)+.20*np.sin(2*np.pi*hz*2*tm))*env
    # Original, deterministic sparse plucked motif; no third-party music.
    melody=[72,76,79,83,79,76,74,79,71,74,79,81,79,74,69,72]
    for n,at in enumerate(np.arange(.12,DURATION-.4,.625)):
        add(at,note(melody[n%len(melody)]),.10,math.sin(n)*.25)
        if n%4==0:
            add(at,note([48,53,55,48][n//4%4],1.3),.10)
    for at in [7.2,7.61,8.03,8.45,8.87,9.28,12.95,13.18,25.53]:
        tm=np.arange(2205)/rate
        click=(rng.normal(0,.7,len(tm))+np.sin(2*np.pi*1150*tm)*.2)*np.exp(-tm*110)
        add(at,click,.18)
    for section,origin in [(0,(650,808)),(20,(570,839))]:
        for n in range(16 if section==0 else 18):
            at=n*.28-.65 if section==0 else section+n*.27
            if section==0 and at>3.5: break
            tm=np.arange(3500)/rate
            noise=rng.normal(0,1,len(tm))
            noise=np.convolve(noise,np.ones(8)/8,"same")*np.sin(np.pi*np.linspace(0,1,len(tm)))**2
            add(at,noise,.10,.25)
            x,y,hit=particle_position(0,n,origin,1400 if section==0 else 1410)
            if (section==0 and at+hit<3.5) or (section>0 and at+hit<25.5):
                with wave.open(str(ROOT/f"assets/audio/impact-soft-{n%3}.wav"),"rb") as stream:
                    raw=np.frombuffer(stream.readframes(stream.getnframes()),dtype="<i2").astype(np.float32)/32768
                    if stream.getframerate()!=rate: raise ValueError("Expected 44100 Hz effects")
                pan=max(-.8,min(.8,(origin[0]+(-185-(n*71%230))*hit-540)/540))
                add(at+hit,raw,.65,pan)
                bounce=2*(-490-(n*83%170)+870*hit)*.42/870
                end=3.5 if section==0 else 25.55
                if at+hit+bounce<end: add(at+hit+bounce,raw,.18,pan)
    add(19.6,note(84,1.4),.22)
    add(19.84,note(88,1.4),.16)
    fade=int(rate*.8)
    sound[:int(rate*.05)]*=np.linspace(0,1,int(rate*.05))[:,None]
    sound[-fade:]*=np.linspace(1,0,fade)[:,None]
    peak=float(np.max(np.abs(sound)))
    sound*=.78/max(peak,.001)
    with wave.open(str(OUT/"zhuguo-original-audio.wav"),"wb") as stream:
        stream.setparams((2,2,rate,0,"NONE","not compressed"))
        stream.writeframes(np.rint(sound*32767).astype("<i2").tobytes())


def subtitles():
    def tc(v):
        ms=round(v*1000)
        return f"00:00:{ms//1000:02d},{ms%1000:03d}"
    contents="\n\n".join(f"{i+1}\n{tc(STARTS[i])} --> {tc(STARTS[i+1])}\n{c}" for i,c in enumerate(CAPTIONS))+"\n"
    (OUT/"zhuguo-captions.srt").write_text(contents,encoding="utf-8-sig")


def stills():
    times=[1.4,4.9,8.7,12.6,17.2,22.1,26.5,29.5,33.6]
    sheet=Image.new("RGB",(1080,2025),"#DFE2D6")
    for i,t in enumerate(times):
        im=frame(t)
        im.save(OUT/f"scene-{i+1:02d}.jpg",quality=93)
        thumb=im.resize((324,576),Image.Resampling.LANCZOS)
        x=27+(i%3)*351
        y=30+(i//3)*665
        sheet.paste(thumb,(x,y))
        text(sheet,f"{STARTS[i]:04.1f}–{STARTS[i+1]:04.1f}s",x,y+590,24,GREEN,latin=True)
    sheet.save(OUT/"zhuguo-storyboard.jpg",quality=95)
    frame(1.4).save(OUT/"zhuguo-poster-9x16.jpg",quality=95)


def render_video():
    import imageio_ffmpeg
    ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
    output=OUT/"zhuguo-xiaohongshu-1080x1920.mp4"
    args=[ffmpeg,"-y","-f","rawvideo","-vcodec","rawvideo","-pix_fmt","rgb24","-s",f"{W}x{H}","-r",str(FPS),"-i","-","-i",str(OUT/"zhuguo-original-audio.wav"),"-c:v","libx264","-preset","fast","-crf","18","-pix_fmt","yuv420p","-c:a","aac","-b:a","192k","-af","loudnorm=I=-18:TP=-1.5:LRA=8","-ar","44100","-movflags","+faststart","-t",str(DURATION),str(output)]
    with (OUT/"encode.log").open("w",encoding="utf-8") as log:
        process=subprocess.Popen(args,stdin=subprocess.PIPE,stderr=log,stdout=subprocess.DEVNULL)
        try:
            for i in range(FPS*DURATION):
                process.stdin.write(frame(i/FPS).tobytes())
                if i%(FPS*3)==0: print(f"Rendered {i//FPS}/{DURATION}s",flush=True)
        finally:
            process.stdin.close()
        if process.wait()!=0: raise RuntimeError("Video encoding failed; see encode.log")
    # Full decode catches truncated streams, missing frames and damaged audio/video.
    verify=subprocess.run([ffmpeg,"-v","error","-i",str(output),"-f","null","-"],capture_output=True,text=True)
    if verify.returncode or verify.stderr.strip(): raise RuntimeError(verify.stderr)
    probe=subprocess.run([ffmpeg,"-hide_banner","-i",str(output),"-f","null","-"],capture_output=True,text=True)
    (OUT/"media-info.txt").write_text(probe.stderr,encoding="utf-8")
    (OUT/"verification.json").write_text(json.dumps({"file":output.name,"duration_seconds":DURATION,"width":W,"height":H,"fps":FPS,"frames_rendered":FPS*DURATION,"video":"H.264 / yuv420p","audio":"AAC stereo / 44100 Hz","full_decode":"PASS","bytes":output.stat().st_size},ensure_ascii=False,indent=2),encoding="utf-8")
    print(f"DONE: {output} ({output.stat().st_size/1024/1024:.1f} MB)",flush=True)


if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--stills",action="store_true")
    args=parser.parse_args()
    OUT.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(Path(__file__).with_name("player.html"), OUT/"index.html")
    shutil.copyfile(Path(__file__).with_name("发布文案.md"), OUT/"发布文案.md")
    cover()
    subtitles()
    stills()
    if not args.stills:
        audio()
        render_video()
