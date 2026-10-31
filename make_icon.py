"""Rebuild the code-drawn crop-frame/chat-bubble application icon."""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parent
im = Image.new('RGBA', (1024, 1024))
d = ImageDraw.Draw(im)
d.rounded_rectangle((32, 32, 992, 992), radius=224, fill='#147D70')
# Four viewfinder corners surround a chat bubble.
for points in [((368,224),(240,224),(240,352)), ((656,224),(784,224),(784,352)),
               ((240,672),(240,800),(368,800)), ((784,672),(784,800),(656,800))]:
    d.line(points, fill='#B7F8D8', width=48, joint='curve')
d.rounded_rectangle((320,352,704,636), radius=72, fill='white')
d.polygon(((416,612),(416,716),(540,612)), fill='white')
for x in (416,512,608):
    d.ellipse((x-22,472,x+22,516), fill='#147D70')
im.resize((256,256),Image.Resampling.LANCZOS).save(root/'app-icon.png')
im.save(root/'app.ico', sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
