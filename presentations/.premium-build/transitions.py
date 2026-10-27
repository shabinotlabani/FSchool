from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import re
root=Path(__file__).parent
with ZipFile(root/'sales-draft.pptx') as src, ZipFile(root/'sales-candidate.pptx','w',ZIP_DEFLATED) as dst:
    for item in src.infolist():
        data=src.read(item.filename)
        if re.fullmatch(r'ppt/slides/slide\d+\.xml',item.filename):
            text=data.decode('utf-8')
            text=re.sub(r'<p:transition\b[^>]*(?:/>|>.*?</p:transition>)','',text)
            transition='<p:transition spd="med" advClick="1"><p:fade/></p:transition>'
            pattern=r'(<p:clrMapOvr\b.*?</p:clrMapOvr>|<p:clrMapOvr\b[^>]*/>)'
            if re.search(pattern,text):text=re.sub(pattern,lambda m:m.group(0)+transition,text,count=1)
            else:text=text.replace('</p:cSld>','</p:cSld>'+transition,1)
            data=text.encode('utf-8')
        dst.writestr(item,data)
print('Added fade transitions to all slides.')
