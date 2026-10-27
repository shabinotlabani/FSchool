import fs from 'node:fs/promises';
import path from 'node:path';
import {pathToFileURL} from 'node:url';
import {Presentation,PresentationFile} from 'file:///C:/Users/OnLine/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const root=process.cwd(), build=path.join(root,'presentations/.build'), out=path.join(root,'presentations/output');
await fs.mkdir(out,{recursive:true});
const p=Presentation.create({slideSize:{width:1280,height:720}});
const ink='#122B38', muted='#58707B', green='#087F68', lime='#B7EF7A', navy='#102D39', paper='#F6F8F6';
const font='Aptos';
function text(s,t,x,y,w,h,size=28,color=ink,bold=false){const a=s.shapes.add({geometry:'textbox',position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});a.text=t;a.text.style={fontFamily:font,fontSize:size,color,bold};return a;}
function slide(title,notes='',dark=false){const s=p.slides.add();s.background.fill=dark?navy:paper;if(title)text(s,title,72,62,1136,110,44,dark?'#FFFFFF':ink,true);text(s,String(p.slides.items.length).padStart(2,'0'),1155,660,60,25,17,dark?'#A5B7BC':muted);s.speakerNotes.text=notes;return s;}
function pairs(s,items,y=220){items.forEach(([h,b],i)=>{let yy=y+i*130;text(s,h,72,yy,380,52,30,green,true);text(s,b,480,yy,720,86,27,ink);});}
function table(s,values,y=220,widths){let t=s.tables.add({rows:values.length,columns:values[0].length,left:72,top:y,width:1136,height:values.length*68,values,columnWidths:widths});for(let r=0;r<values.length;r++)for(let c=0;c<values[0].length;c++){let cell=t.getCell(r,c);cell.fill=r===0?navy:(r%2?'#FFFFFF':'#EAF1ED');cell.text.style={fontFamily:font,fontSize:24,color:r===0?'#FFFFFF':ink,bold:r===0};}return t;}

let s=slide('', 'Hapja: Pyetni drejtuesin cili proces i merr më shumë kohë sot. Lidheni prezantimin me përgjigjen. Ky prezantim përshkruan funksionet e platformës së zhvilluar për klubin. Nuk përmban statistika tregu ose rezultate të garantuara.',true);
text(s,'PLATFORMË PËR KLUBE DHE AKADEMI FUTBOLLI',76,96,1120,40,22,lime,true);
text(s,'Menaxhimi i klubit\nnë një platformë',72,205,1120,210,68,'#FFFFFF',true);
text(s,'Puna e përditshme e klubit, e lidhur me lojtarin.',76,475,1060,70,30,'#C8D8DD');
text(s,'Prezantim për drejtuesit e klubeve',76,600,950,35,22,'#C8D8DD');

s=slide('Informacioni i shpërndarë e vështirëson punën','Këto janë situata që prezantuesi duhet t’i verifikojë me klubin, jo pretendime se çdo klub i ka. Pyetni ku mbahen aktualisht prezencat, pagesat dhe vlerësimet.');
pairs(s,[['Në stërvitje','Prezenca dhe vlerësimet mund të mbeten në shënime të veçuara.'],['Në administratë','Pagesat dhe dokumentet kërkojnë kontroll mes disa listave.'],['Me prindërit','Orari dhe pyetjet e përsëritura kalojnë nëpër mesazhe.']]);

s=slide('Një kartelë që e lidh punën e klubit','Burimi: Views/Payments/Account.cshtml, Views/Payments/Progress.cshtml dhe funksionet e zhvilluara në projekt. Qasja varet nga roli. Nuk pretendohet se çdo përdorues sheh të gjitha të dhënat.',true);
text(s,'LOJTARI',72,220,440,90,66,lime,true);
text(s,'Ekipi dhe prezenca\nPagesat dhe rekuizitat\nVlerësimet dhe matjet\nKujdesi dhe menuja',600,210,590,300,35,'#FFFFFF');
text(s,'Secili rol ka qasjen që i takon.',76,570,1100,60,30,'#C8D8DD');

s=slide('Çfarë fiton secili rol','Burimi: Controllers/CoachesController.cs, ParentPortalService, PlayerCareService dhe raportet e projektit. Nuk përfshihet moduli i psikologut, i cili mbetet për zhvillim të ardhshëm.');
table(s,[['Roli','Puna në platformë'],['Drejtuesi','Raportet, financat dhe aktiviteti i trajnerëve'],['Trajneri','Prezenca dhe zhvillimi i lojtarëve'],['Prindi','Orari, pagesat, ecuria dhe njoftimet'],['Mjeku / nutricisti','Rastet, udhëzimet dhe menuja e lojtarit']],205,[330,806]);

s=slide('Trajneri punon nga telefoni ose tableti','Burimi: Views/Coaches/RoleChoice.cshtml, Portal.cshtml, Attendance dhe PlayerDevelopment. Në prezantimin praktik hapni ekipin, shënoni prezencën dhe regjistroni një matje pa përdorur të dhëna personale reale.');
pairs(s,[['Ekipi i tij','Orari dhe lista e lojtarëve në një vend.'],['Prezenca','Regjistrim për seancën dhe historik i pjesëmarrjes.'],['Kartela e lojtarit','Pozicioni, matjet dhe vlerësimet pranë lojtarit.']]);

s=slide('Zhvillimi i lojtarit bëhet i dukshëm','Burimi: Models/DevelopmentRecord.cs dhe grafikat e PlayerDevelopment. Funksionet ruajnë matjet dhe vlerësimet. Mos premtoni identifikim automatik apo të garantuar të talenteve.');
text(s,'Historik për çdo lojtar',72,212,560,125,48,green,true);
text(s,'Matje fizike dhe testime\nAftësi teknike dhe taktike\nVlerësime të veçanta për portierin',680,220,520,235,29);
text(s,'Grafikët ndihmojnë trajnerin të ndjekë ndryshimet me kalimin e kohës.',72,490,1100,110,34);

s=slide('Prindi e ka informacionin në telefon','Burimi: Views/ParentPortal dhe ParentAutomationService. Njoftimet push kërkojnë aktivizim dhe lejen e pajisjes. Disponueshmëria e funksioneve varet nga konfigurimi.');
pairs(s,[['Portali i familjes','Ekipi, trajneri, orari dhe shumat për t’u paguar.'],['Ecuria dhe menuja','Vlerësime, menuja javore dhe udhëzimet e ndara nga stafi.'],['Njoftimet','Kujtesa para stërvitjes, ndryshime orari dhe pagesa.']]);
text(s,'Njoftimet kërkojnë aktivizimin nga prindi.',72,626,1050,30,19,muted);

s=slide('Pagesat pasqyrojnë shumën reale','Burimi: rrjedha e verifikimit bankar, Payments dhe Sales. Shembulli është ilustrues. Tregoni në sistem ndarjen e pagesave sipas detyrimeve dhe trajtimin e tepricës.');
text(s,'100',72,208,300,110,76,ink,true);text(s,'Për t’u paguar',76,335,340,50,27,muted);
text(s,'60 ose 110',555,208,650,110,76,green,true);text(s,'Operatori regjistron shumën e pranuar në bankë',560,335,620,100,27);
text(s,'Kartela ruan pagesat, detyrimet dhe parapagimin.',76,500,1100,80,34);
text(s,'Shembull ilustrues',76,622,1000,30,19,muted);

s=slide('Sponsorizimet kanë buxhetin e tyre','Burimi: moduli Sponsorship, plani vjetor dhe raporti i mbulimit. Vlerat janë shembulli i dhënë nga përdoruesi, jo të dhëna reale të klubit. Shpenzimet e sponsorizimeve mbahen të ndara nga shpenzimet operative të klubit.');
table(s,[['Kategoria','Plani vjetor','Sponsor','Mbeten për mbulim'],['Pagat','20 000','2 000','18 000']],225,[280,250,240,366]);
text(s,'Kontrata dhe afatet e përdorimit\nFaturat sipas kategorive\nRaporti për secilin sponsor',76,420,1100,170,30);
text(s,'Shembull ilustrues i mbulimit të planit',76,625,1100,30,19,muted);

s=slide('Kontrolle për regjistrime më të sakta','Burimi: kontrollet e sponsorizimit dhe AttendanceService. Kontrolli i pagës për të njëjtin person dhe muaj shmang financimin e dyfishtë nga sponsorë. Nuk është garanci për përputhje ligjore ose auditim të pavarur.');
pairs(s,[['Faturat','Numri i njëjtë dhe furnitori i njëjtë nuk regjistrohen dy herë.'],['Pagat','I njëjti person dhe muaj nuk financohen dy herë nga sponsorë.'],['Kufizimi mjekësor','Kur mjeku cakton pushim, trajneri nuk shënon prezencë.']]);

s=slide('Drejtuesi sheh aktivitetin e trajnerëve','Burimi: Services/CoachActivityService.cs dhe Views/Reports/CoachActivity.cshtml. Një regjistrim unik jep një pikë. Numërohen prezencat, regjistrimet e zhvillimit dhe rastet/kërkesat. Rezultati mat përdorimin, jo cilësinë e trajnerit. Madhësia e ekipit ndikon në total.');
text(s,'1 regjistrim unik\n= 1 pikë',72,225,590,190,56,green,true);
text(s,'Prezenca\nVlerësime dhe matje\nRaste dhe kërkesa',750,225,450,180,30);
text(s,'Ditët aktive dhe regjistrimi i fundit për çdo trajner.',76,500,1100,85,34);
text(s,'Korrigjimet nuk shtojnë pikë. Pikët matin përdorimin e programit.',76,624,1100,40,20,muted);

s=slide('Përshtatja me klubin tuaj','Burimi: konfigurimi i të dhënave të klubit dhe roleve. Ky është rend i propozuar për zbatimin me klientin, jo afat ose shërbim kontraktual i garantuar. Mos premtoni import automatik pa verifikim.');
pairs(s,[['01  Klubi','Emri, logoja, ekipet dhe llogaritë e stafit.'],['02  Puna ditore','Lojtarët, tarifat, oraret dhe portali i prindit.'],['03  Prova praktike','Një ekip teston rrjedhën para shtrirjes në gjithë klubin.']]);

s=slide('Çfarë vlerësojmë gjatë provës','Këto janë kritere të propozuara pranimi. Nuk paraqiten si rezultate të arritura ose matje ekzistuese. Dakordoni me klubin çfarë do të quhet sukses dhe pastaj formuloni ofertën.');
text(s,'A e kryen stafi punën më lehtë?',72,215,1136,75,40,green,true);
text(s,'Trajneri regjistron një seancë dhe një vlerësim.\nAdministrata verifikon një pagesë reale.\nPrindi gjen orarin dhe informacionin e fëmijës.\nDrejtuesi lexon raportin pa kërkuar lista shtesë.',76,335,1120,250,29);

s=slide('', 'Mbyllja: Propozoni një demonstrim me një ekip të klubit. Dakordoni personin përgjegjës, modulet dhe kohën e provës. Çmimi, migrimi, mbështetja dhe kushtet vendosen në ofertë të veçantë. Nuk ka çmim ose kontakt të sajuar.',true);
text(s,'Hapi i radhës',76,110,1050,60,30,lime,true);
text(s,'Një prezantim praktik\nme ekipin tuaj',72,220,1136,200,64,'#FFFFFF',true);
text(s,'Zgjedhim rrjedhën që ka më shumë rëndësi për klubin.\nPastaj përcaktojmë modulet dhe ofertën.',76,485,1100,115,30,'#C8D8DD');

await (await PresentationFile.exportPptx(p)).save(path.join(build,'candidate.pptx'));
for(let i=0;i<p.slides.items.length;i++){const b=await p.export({slide:p.slides.items[i],format:'png',scale:1});await fs.writeFile(path.join(build,`slide-${String(i+1).padStart(2,'0')}.png`),new Uint8Array(await b.arrayBuffer()));}
const skill='C:/Users/OnLine/.codex/plugins/cache/openai-primary-runtime/presentations/26.1015.12040/skills/presentations';
const {finalizePresentation}=await import(pathToFileURL(path.join(skill,'container_tools/artifact_tool_utils.mjs')).href);
const result=await finalizePresentation({workspaceDir:root,candidatePath:path.join(build,'candidate.pptx'),finalPath:path.join(out,'Platforma-per-Klube.pptx'),pythonExecutable:'C:/Users/OnLine/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe',integrityValidatorPath:path.join(skill,'container_tools/inspect_presentation_package_integrity.py'),layoutValidatorPath:path.join(skill,'container_tools/inspect_presentation_layout_geometry.py'),layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-heading-fit'],requiredNativeTableOwnerSlides:[],fontPolicy:{basis:'design',families:[font]},verifyArtifactToolImport:true,receiptPath:path.join(build,'validation.json')});
console.log(JSON.stringify(result));
