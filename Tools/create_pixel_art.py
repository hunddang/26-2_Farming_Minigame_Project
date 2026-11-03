"""Original, deterministic pixel artwork for Night Cafe. No model/API is used.

Run with Python + Pillow. Final PNGs are imported as ordinary Unity sprites.
"""
from pathlib import Path
import math
import random
from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parents[1] / 'Unity/Assets/Art/NightCafe'
OUT.mkdir(parents=True, exist_ok=True)
INK = '#111b20'
EDGE = '#1c292b'
PAPER = '#e2d8b6'
GOLD = '#d5a969'
TEAL = '#5d9a8d'
WOOD = '#85634b'
random.seed(18)

def save(im, name):
    im.save(OUT / (name + '.png'))

def canvas(w, h):
    im = Image.new('RGBA', (w, h))
    return im, ImageDraw.Draw(im)

def box(d, bounds, fill, edge=EDGE, width=2):
    d.rectangle(bounds, fill=fill, outline=edge, width=width)

im, d = canvas(8, 8)
d.rectangle((0, 0, 7, 7), fill='white')
save(im, 'pixel')
im, d = canvas(64, 64)
d.ellipse((1, 1, 62, 62), fill='white')
save(im, 'circle')
im, d = canvas(128, 192)
for y in range(192):
    half = y / 192 * 64
    d.line((64 - half, y, 64 + half, y), fill=(255, 249, 212, int(150 * (1 - y / 192))))
save(im, 'flashlight')

for name, base, seam in [('floor-wood', (79, 66, 53), (49, 48, 43)), ('floor-tile', (43, 58, 57), (31, 44, 44))]:
    im = Image.new('RGBA', (128, 128), (*base, 255)); d = ImageDraw.Draw(im)
    if name == 'floor-wood':
        for y in range(0, 128, 16):
            d.line((0, y, 128, y), fill=seam, width=2)
            for x in range((y // 16 % 2) * 32 - 64, 128, 64):
                d.line((x, y, x, y + 15), fill=seam)
        for _ in range(180):
            x, y = random.randrange(128), random.randrange(128)
            d.line((x, y, min(x + random.randrange(2, 14), 127), y), fill=(base[0]+7,base[1]+6,base[2]+3))
    else:
        for y in range(0, 128, 32):
            for x in range(0, 128, 32):
                b = 3 if ((x+y)//32)%2 else 0
                d.rectangle((x+1,y+1,x+30,y+30),fill=(*[v+b for v in base],255))
                d.line((x+2,y+2,x+29,y+2),fill=(57,72,68))
        for x in range(0, 128, 32): d.line((x,0,x,128),fill=seam)
        for y in range(0, 128, 32): d.line((0,y,128,y),fill=seam)
    save(im, name)

im, d = canvas(128, 32)
box(d,(0,0,127,31),'#66736a')
d.rectangle((3,3,124,10),fill='#9a9c7f'); d.rectangle((3,23,124,29),fill='#344744')
for x in range(15,128,32): d.line((x,11,x,22),fill='#465955',width=2)
save(im,'wall')

for name, w, h in [('table',96,64),('counter',128,32),('desk',128,48),('shelf',48,128)]:
    im,d=canvas(w,h)
    box(d,(1,4,w-2,h-1),'#2b2927'); box(d,(1,1,w-2,h-8),WOOD)
    d.rectangle((5,4,w-6,8),fill='#b29469')
    for y in range(14,h-8,10):
        d.line((5,y,w-7,y),fill='#76523f')
        d.line((w//3,y+2,min(w-8,w//3+24),y+2),fill='#9e7954')
    if name=='table':
        d.rectangle((w//2-9,7,w//2+8,h-12),fill='#b1aa84')
        d.line((w//2-6,7,w//2-6,h-12),fill='#d2c99e')
    if name=='shelf':
        for y in range(15,h-13,23):
            box(d,(7,y,w-8,y+15),'#393c35',width=1)
            for x in range(10,w-10,10):
                box(d,(x,y+4,x+6,y+12),random.choice(['#999b80','#597c70','#b8aa7d']),width=1)
    save(im,name)

im,d=canvas(40,40)
box(d,(3,0,36,37),'#41665f');box(d,(7,3,32,11),'#709081')
box(d,(6,15,33,31),'#52796b'); d.line((8,30,31,30),fill='#88a08a',width=2)
save(im,'chair')

im,d=canvas(64,112)
box(d,(4,2,59,109),'#554b42'); box(d,(7,8,56,99),'#aaab91')
box(d,(11,10,52,29),'#d5ccb0'); box(d,(9,36,54,96),'#527870')
for y in range(38,94,10): d.line((13,y,51,y),fill='#67877b',width=2)
d.line((10,97,53,97),fill='#99aa8e',width=2)
save(im,'bed')

im,d=canvas(64,64)
d.ellipse((13,36,52,61),fill=EDGE);box(d,(21,37,45,59),'#a07852');d.line((24,42,42,42),fill='#c39c6e',width=3)
for x,y,rx,ry in [(19,15,10,18),(40,14,10,16),(31,6,10,18),(44,29,13,9),(14,32,12,9),(29,24,10,16)]:
    d.ellipse((x-rx,y-ry,x+rx,y+ry),fill='#416755',outline=EDGE,width=2)
    d.line((x,y,x+3,y-6),fill='#779372',width=2)
save(im,'plant')

im,d=canvas(80,60)
box(d,(4,3,75,47),'#313e3d');box(d,(9,8,70,39),'#79aa8c')
d.rectangle((13,12,66,35),fill='#1c4843')
for y in range(14,36,4): d.line((13,y,66,y),fill='#285951')
for x,y in [(16,15),(42,15),(16,27),(42,27)]:
    box(d,(x,y,x+19,y+8),'#315f52',edge='#669077',width=1)
d.rectangle((34,48,46,53),fill='#596c60');box(d,(23,54,57,58),'#3c4e48',width=1)
d.rectangle((64,43,67,44),fill='#bada91')
save(im,'monitor')

for name, color in [('note','#d4b570'),('recipe','#cbbd98'),('book','#6a9083')]:
    im,d=canvas(48,60)
    box(d,(3,3,44,56),color)
    if name=='book':
        d.rectangle((6,4,10,55),fill='#304c47');box(d,(16,14,36,35),'#b4c0a0',width=1)
        d.line((20,22,32,22),fill='#536e60',width=2)
    else:
        d.polygon([(32,3),(44,3),(44,15)],fill='#eee1b8')
        for y in range(15,47,7):d.line((10,y,32 if y%2 else 36,y),fill='#776b52',width=2)
    save(im,name)

im,d=canvas(64,64)
d.ellipse((3,13,60,57),fill=(10,18,19,80))
d.ellipse((39,19,62,41),fill='#c7c7a9',outline=EDGE,width=3)
d.ellipse((44,24,57,36),fill=(0,0,0,0))
d.ellipse((5,5,49,50),fill='#c6cbb8',outline=EDGE,width=2)
d.ellipse((10,9,44,41),fill='#e4dec1')
d.ellipse((14,12,41,38),fill='#3b2b22');d.arc((16,14,38,35),20,220,fill='#896645',width=2)
d.arc((6,6,48,49),50,135,fill='#f4e6c6',width=3)
save(im,'cup')

for name, accent in [('milk','#7daba6'),('chocolate','#9d6a50'),('almond','#c2a274'),('tapioca','#90809a'),('cream','#8d9e76')]:
    im,d=canvas(48,64)
    if name in ('milk','cream'):
        d.polygon([(10,14),(19,5),(35,5),(40,15),(40,58),(10,58)],fill=PAPER,outline=EDGE,width=2)
        d.polygon([(11,15),(39,15),(34,6),(19,6)],fill=accent)
        box(d,(11,31,39,48),accent,edge=accent,width=1)
        d.rectangle((18,34,31,44),fill='#dedcc2')
        d.line((34,18,34,55),fill='#aaa98e',width=2)
    elif name=='chocolate':
        box(d,(7,7,40,58),'#79513f');box(d,(9,8,38,20),'#e0c88c',width=1)
        for x in (12,25):
            for y in (25,39): box(d,(x,y,x+10,y+11),'#a27a55',width=1)
    else:
        box(d,(10,13,39,59),'#bcb89e');box(d,(7,7,41,16),'#556a5c')
        box(d,(13,28,36,47),accent,width=1)
        for x,y in [(18,34),(28,38),(22,43)]:d.ellipse((x-3,y-3,x+3,y+3),fill='#615143')
    save(im,name)

im,d=canvas(48,64)
d.ellipse((7,49,41,60),fill=(9,17,20,110))
box(d,(11,28,36,48),'#466d61');box(d,(10,46,20,55),'#263d3c',width=1);box(d,(28,46,37,55),'#263d3c',width=1)
box(d,(9,31,14,43),'#a9ad91',width=1);box(d,(34,31,39,43),'#a9ad91',width=1)
save(im,'player-body')
im,d=canvas(48,48)
d.ellipse((11,9,36,36),fill=EDGE);d.ellipse((14,13,33,34),fill='#c4ad85')
d.polygon([(12,14),(16,7),(29,7),(36,16),(35,23),(30,18),(14,19)],fill='#363c35')
d.rectangle((18,28,20,30),fill='#4e5041');d.rectangle((29,28,31,30),fill='#4e5041')
save(im,'player-head')

im,d=canvas(128,32)
box(d,(0,1,127,30),'#36534c');d.rectangle((4,5,123,25),fill='#537368')
for x in range(6,124,12):d.line((x,5,x,25),fill='#3e6157')
d.rectangle((96,12,112,16),fill=GOLD)
save(im,'door')

for name, fill in [('panel','#1e302f'),('button','#304541'),('button-selected','#d5b879'),('paper','#c6bc98')]:
    im,d=canvas(64,64)
    box(d,(0,0,63,63),fill,edge='#0d1c20',width=2)
    d.line((3,3,60,3),fill='#587064' if name!='paper' else '#e1d5ad')
    d.line((3,3,3,60),fill='#40574e' if name!='paper' else '#d7cba5')
    d.line((3,60,60,60),fill='#142522' if name!='paper' else '#998f72')
    save(im,name)

# Layered, hand-authored title illustration. Drawn at pixel resolution, enlarged nearest.
im=Image.new('RGB',(640,360),'#102024');d=ImageDraw.Draw(im)
for y in range(360):
    t=y/360
    d.line((0,y,640,y),fill=(int(16+8*t),int(29+7*t),int(32+5*t)))
box(d,(278,34,627,230),'#253d3b',edge='#0b1b1e',width=5)
for x in (289,400,511):
    box(d,(x,44,x+101,217),'#193136',width=3)
    for _ in range(65):
        xx,yy=random.randrange(x+3,x+97),random.randrange(48,208)
        d.line((xx,yy,xx-3,yy+12),fill=random.choice(['#345552','#284849','#41645a']))
    d.rectangle((x+3,174,x+97,214),fill='#213c38')
for x in range(293,608,17):
    h=random.randrange(20,46);d.rectangle((x,213-h,x+8,213),fill='#29463e')
box(d,(285,220,620,233),'#44574a')
for x in (325,450,570):
    d.line((x,0,x,31),fill='#102022',width=3)
    d.polygon([(x-19,46),(x-12,32),(x+12,32),(x+19,46)],fill='#30443b',outline='#0d1b1c',width=2)
    d.line((x-14,47,x+14,47),fill='#bdaa72',width=3)
    d.polygon([(x-12,50),(x+12,50),(x+61,233),(x-61,233)],fill='#2c4438')
# Restore window shapes through muted light using sparse dither.
for _ in range(700):
    x=random.randrange(300,620);y=random.randrange(54,232)
    if abs(x-450)<(y-40)*.35:d.point((x,y),fill='#3a5345')
d.polygon([(260,253),(640,253),(640,360),(242,360)],fill='#27352e')
for y in range(261,360,16):
    d.line((260,y,640,y),fill='#374034',width=2)
for x in range(272,640,64):d.line((x,253,x-30,360),fill='#1b2a26',width=2)
box(d,(247,235,639,281),'#775b43',width=3)
d.rectangle((250,239,638,246),fill='#a08759')
d.rectangle((252,251,639,257),fill='#4c4937')
box(d,(455,197,608,235),'#243a37',width=3)
for x in range(466,604,27):
    box(d,(x,210,x+15,228),'#a6a38a',width=1)
    d.arc((x+8,213,x+21,224),250,110,fill='#ada98e',width=2)
# Ceramic mug, suspended above the worktop, and its impossible coffee trail.
d.ellipse((327,253,445,268),fill='#343b30')
d.ellipse((393,151,445,198),fill='#bac2aa',outline='#172a28',width=4)
d.ellipse((406,161,433,187),fill='#25403b')
d.polygon([(336,150),(414,150),(407,205),(398,215),(355,213),(342,201)],fill='#c5c7a9',outline='#1d302a',width=3)
d.polygon([(344,160),(356,164),(363,204),(350,199)],fill='#e4dab4')
d.ellipse((334,139,416,170),fill='#e1d6af',outline='#30443a',width=3)
d.ellipse((344,145,407,164),fill='#392d23')
d.arc((348,148,400,163),195,345,fill='#8c6946',width=2)
for pts in [[(359,142),(350,122),(368,104),(364,88)],[(386,141),(395,124),(380,103),(388,83)]]:
    d.line(pts,fill='#789882',width=2)
for i in range(13):
    a=i*.7;x=375+int(math.cos(a)*(34+i*2));y=224+int(math.sin(a)*12+i*2)
    d.rectangle((x,y,x+3,y+5),fill='#aa8451')
d.ellipse((338,224,415,235),outline='#668371',width=1)
# Desk plant and an abandoned notebook.
d.rectangle((573,180,595,205),fill='#947351')
for x,y,r in [(568,166,13),(584,149,15),(601,164,14),(584,175,12)]:
    d.ellipse((x-r,y-r,x+r,y+r),fill='#496b53',outline='#243c32',width=2)
d.polygon([(464,249),(507,245),(520,266),(476,273)],fill='#bab18b',outline='#3b4535',width=2)
for y in range(252,265,4):d.line((476,y,501,y-3),fill='#777d5f')
# Fine grain stays subtle; left half is deliberately quiet for editable UI text.
for _ in range(3000):
    x,y=random.randrange(640),random.randrange(360)
    r,g,b=im.getpixel((x,y));delta=random.choice([-3,2])
    d.point((x,y),fill=(max(0,r+delta),max(0,g+delta),max(0,b+delta)))
px=im.load()
for x in range(640):
    opacity=min(.96,max(0,(330-x)/210))
    for y in range(360):
        r,g,b=px[x,y];px[x,y]=(int(r*(1-opacity)+12*opacity),int(g*(1-opacity)+22*opacity),int(b*(1-opacity)+26*opacity))
save(im.resize((1920,1080),Image.Resampling.NEAREST),'title-cafe')

sprites=[]
for path in sorted(OUT.glob('*.png')):
    if path.stem in ('title-cafe','pixel','flashlight'):continue
    thumb=Image.open(path).convert('RGBA');thumb.thumbnail((110,100),Image.Resampling.NEAREST)
    sprites.append((path.stem,thumb))
sheet=Image.new('RGB',(720,math.ceil(len(sprites)/6)*142),'#142428');d=ImageDraw.Draw(sheet)
for i,(name,thumb) in enumerate(sprites):
    x=(i%6)*120;y=(i//6)*142
    sheet.paste(thumb,(x+(120-thumb.width)//2,y+8),thumb)
    d.text((x+7,y+116),name,fill='#ddd4b3')
save_path=Path(__file__).resolve().parents[1]/'Artifacts/art-contact-sheet.png'
save_path.parent.mkdir(exist_ok=True);sheet.save(save_path)
print(f'Created {len(list(OUT.glob("*.png")))} PNG assets at {OUT}')
