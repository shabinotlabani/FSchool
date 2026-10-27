import fs from 'node:fs/promises';
import {readFileSync} from 'node:fs';
import path from 'node:path';
import {Presentation,PresentationFile} from 'file:///C:/Users/OnLine/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const root=process.cwd(),build=path.join(root,'presentations/.premium-build'),out=path.join(root,'presentations/Premium_Sales'),assets=path.join(out,'Presentation_Assets');
await fs.mkdir(path.join(assets,'Slide_Previews'),{recursive:true});
const p=Presentation.create({slideSize:{width:1280,height:720}});
const C={navy:'#101D30',ink:'#17263D',gold:'#F5CE4F',gray:'#68788C',paper:'#F6F8FB',line:'#DCE3EC',blue:'#326CB0',green:'#137B66',white:'#FFFFFF'};
const font='Aptos';
const sourceIcons=await fs.readFile(path.join(root,'Views/Shared/_Icons.cshtml'),'utf8');
function txt(s,t,x,y,w,h,size=28,color=C.ink,bold=false){const a=s.shapes.add({name:t.slice(0,40),geometry:'textbox',position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});a.text=t;a.text.style={typeface:font,fontSize:size,color,bold,insets:0};return a;}
function shape(s,x,y,w,h,fill=C.white,geom='roundRect'){return s.shapes.add({geometry:geom,position:{left:x,top:y,width:w,height:h},fill,line:{fill:C.line,width:1},borderRadius:16});}
function card(s,t,x,y,w=240,h=108,fill=C.white,color=C.ink,size=25){let a=shape(s,x,y,w,h,fill);a.text=t;a.text.style={typeface:font,fontSize:size,bold:true,color,insets:18,verticalAlignment:'middle'};return a;}
function link(s,a,b,from='right',to='left',color=C.blue){return s.shapes.connect(a,b,{kind:'elbow',fromSide:from,toSide:to,line:{fill:color,width:2.5},tail:{type:'triangle',width:'sm',length:'sm'}});}
function image(s,file,x,y,w,h,fit='cover'){return s.images.add({...(file.endsWith('.svg')?{svg:readFileSync(file,'utf8')}:{blob:new Uint8Array(readFileSync(file)),contentType:'image/png'}),position:{left:x,top:y,width:w,height:h},fit,...(/club-office|parent-player/.test(file)?{crop:{left:0.25,top:0,right:0,bottom:0}}:{}),alt:path.basename(file)});}
function icon(s,id,x,y,color=C.blue){const m=sourceIcons.match(new RegExp(`<symbol id="i-${id}"[^>]*>([\\s\\S]*?)</symbol>`));if(m)s.images.add({svg:`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="${color}" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round">${m[1]}</svg>`,position:{left:x,top:y,width:38,height:38}});}
function slide(title,sub='',notes='',dark=false){const s=p.slides.add();s.background.fill=dark?C.navy:C.paper;if(title)txt(s,title,64,58,1152,100,43,dark?C.white:C.ink,true);if(sub)txt(s,sub,66,157,1140,64,25,dark?'#CBD5E2':C.gray);txt(s,String(p.slides.items.length).padStart(2,'0'),1175,666,42,24,17,dark?'#9DAEC3':C.gray);s.speakerNotes.text=notes;return s;}
function flow(s,labels,y=300){const w=(1152-(labels.length-1)*38)/labels.length;const boxes=labels.map((t,i)=>card(s,t,64+i*(w+38),y,w,120,i===labels.length-1?C.navy:C.white,i===labels.length-1?C.white:C.ink,24));for(let i=0;i<boxes.length-1;i++)link(s,boxes[i],boxes[i+1]);return boxes;}
function takeaway(s,t,y=584){txt(s,t,66,y,1148,64,29,C.blue,true);}
function rows(s,data,y=240,gap=116){for(let i=0;i<data.length;i++){txt(s,data[i][0],66,y+i*gap,330,72,28,C.blue,true);txt(s,data[i][1],430,y+i*gap,776,88,26);}}
function table(s,vals,y=230,widths){const t=s.tables.add({rows:vals.length,columns:vals[0].length,left:64,top:y,width:1152,height:vals.length*59,values:vals,columnWidths:widths});for(let r=0;r<vals.length;r++)for(let c=0;c<vals[0].length;c++){const z=t.getCell(r,c);z.fill=r===0?C.navy:(r%2?C.white:'#EEF2F7');z.text.style={typeface:font,fontSize:23,color:r===0?C.white:C.ink,bold:r===0};}return t;}
const photo=n=>path.join(out,'Generated_Images',n);
let s=slide('','','Burimi: analiza e kodit lokal në Product_Feature_Analysis.md. Foto origjinale e gjeneruar me AI, skenë ilustruese. Logoja e FC 2 Korrikut është identitet shembull i klubit, jo markë produkti e konfirmuar.',true);
image(s,photo('hero-coach.png'),0,0,1280,720);image(s,path.join(assets,'club-logo.svg'),64,44,70,84,'contain');txt(s,'PLATFORMË PËR KLUBE FUTBOLLI',155,67,640,45,22,C.gold,true);
txt(s,'Klubi juaj.\nPuna e lidhur.',64,236,650,190,64,C.white,true);txt(s,'Lojtarët, financat dhe stafi\nnë një platformë.',66,456,620,100,32,'#E4EBF2');txt(s,'Identitet i konfigurueshëm për klubin tuaj',66,640,720,35,20,'#CFD8E5');

s=slide('Informacioni i shpërndarë\ne ngadalëson klubin','', 'Situata për diskutim me klientin, jo pretendime universale. Pyetni si menaxhohet sot regjistrimi, pagesa dhe komunikimi. Foto origjinale AI.');
image(s,photo('club-office.png'),630,200,586,430);txt(s,'Lista të ndara',66,245,510,48,32,C.blue,true);txt(s,'Kërkim i të njëjtit informacion\nnë disa vende.',66,305,510,90,27);txt(s,'Vendime pa pamje të plotë',66,437,510,90,32,C.blue,true);txt(s,'Stafi dhe drejtuesi duhet\ntë krahasojnë shënimet.',66,537,510,80,27);

s=slide('Një platformë për punën e përditshme','Lidh administratën me punën sportive dhe prindin.','Burimi: Controllers, Services, ApplicationDbContext. Përfitimet janë cilësore, jo kursime të matura.');
[['Lojtarët','Kartelë, ekip dhe familje','players'],['Puna sportive','Prezencë dhe zhvillim','calendar'],['Financat','Detyrime, arkëtime dhe sponsorë','wallet'],['Komunikimi','Portal familjar dhe njoftime','home']].forEach((a,i)=>{let x=64+(i%2)*600,y=250+Math.floor(i/2)*170;icon(s,a[2],x,y);txt(s,a[0],x+58,y,480,45,31,C.ink,true);txt(s,a[1],x+58,y+60,480,70,27);});

s=slide('Si lidhen modulet','Lojtari është pika e përbashkët e proceseve.','Burimi: lidhjet e ApplicationDbContext, Student, Payment, DevelopmentRecord, CareAssignment. Sponsorizimet kanë regjistër financiar të ndarë. Diagram konceptual funksional, jo skemë databaze.');
let mid=card(s,'LOJTARI\nKartela dhe familja',465,335,350,118,C.navy,C.white,27);
let a=card(s,'Ekipi dhe orari',64,245,300,100),b=card(s,'Prezenca / zhvillimi',64,435,300,100),c=card(s,'Pagesat / rekuizitat',916,245,300,100),d=card(s,'Mjeku / nutricisti',916,435,300,100),e=card(s,'Portali i prindit',465,535,350,90);
link(s,a,mid);link(s,b,mid);link(s,mid,c);link(s,mid,d);link(s,mid,e,'bottom','top');txt(s,'Sponsorizime dhe arka: buxhete e raporte të veçanta për drejtuesin.',66,650,1100,28,21,C.gray);

s=slide('Rrjedha e klubit në platformë','Nga konfigurimi te informacioni për vendimmarrje.','Burimi: ClubSettings, Teams, Students, Tariffs, Attendance, Reports. Rendi është propozim operimi bazuar në modulet ekzistuese.');
flow(s,['Klubi\ndhe stafi','Ekipet\ndhe tarifat','Lojtarët\ndhe familjet','Puna\ne përditshme','Raportet\ne drejtuesit']);takeaway(s,'Të dhënat e regjistruara ushqejnë proceset e lidhura.');

s=slide('Regjistrimi fillon një kartelë të lidhur','Një lojtar ose disa fëmijë të së njëjtës familje.','Burimi: StudentWorkflowService.RegisterFamilyAsync, FamilyService, ParentLinksController. Linku i portalit krijohet nga personeli i autorizuar; nuk pretendohet dërgim automatik SMS.');
flow(s,['Administrata\nregjistron','Cakton ekipin\ndhe tarifën','Sistemi krijon\nkartelën','Stafi krijon\nlinkun / QR']);
txt(s,'Për administratën',66,510,450,50,28,C.blue,true);txt(s,'Kontakti, detyrimet dhe historia e lojtarit mbeten të lidhura.',520,510,690,100,29);

s=slide('Paneli i trajnerit, gati për terren','Ekipet dhe veprimet kryesore pranë njëra-tjetrës.','Pamje e ndërfaqes origjinale Views/Coaches/RoleChoice.cshtml, renderuar në host të izoluar. Burimi i rrjedhës: AttendanceService. Nuk u përdor databaza e klubit.');
image(s,path.join(assets,'coach-screenshot.png'),64,225,1152,295,'contain');flow(s,['Zgjedh ekipin','Shënon prezencën','Sistemi ruan historikun','Raportimi përditësohet'],540);

s=slide('Zhvillimi i lojtarit ka histori','Matje, aftësi dhe testime sipas datës.','Burimi: DevelopmentSections, PlayerDevelopmentService, ParentProgressService. Fushat përfshijnë teknikë, taktikë, portier, ndeshje, ngarkesë dhe matje. Vetëm pjesa e lejuar publikohet te prindi. Nuk pretendohet vlerësim automatik i talentit.');
flow(s,['Profili\ndhe pozita','Trajneri fut\nmatjet / testet','Sistemi ruan\nhistorikun','Grafikët tregojnë\necurinë']);
txt(s,'Lojtar',66,520,270,55,34,C.blue,true);txt(s,'Pozita kryesore dhe dytësore',335,520,835,55,28);txt(s,'Portier',66,590,270,55,34,C.green,true);txt(s,'Fusha vlerësimi të përshtatura për portierin',335,590,835,55,28);

s=slide('Pagesa bankare kalon në verifikim','Operatori regjistron shumën që është pranuar realisht.','Burimi: BankPaymentsController.Review, BankVerificationService.Distribution. Pagesa bankare nuk është integrim automatik me bankën. Ndarja sipas detyrimeve dhe teprica varen nga rasti/lloji i pagesës dhe validimet e sistemit.');
let p1=card(s,'Pagesa\nnë pritje',64,285,235,110),p2=card(s,'Shuma reale\ndhe data',355,285,245,110),p3=card(s,'Konfirmo?',665,285,235,110,'#FFF2BE'),p4=card(s,'Ndarja dhe\nkartela',965,285,251,110,C.navy,C.white);link(s,p1,p2);link(s,p2,p3);link(s,p3,p4);txt(s,'Po',907,310,50,35,22,C.green,true);let reject=card(s,'Refuzo me arsye',665,490,235,85,'#FBECEA',C.ink,23);link(s,p3,reject,'bottom','top','#BD6862');txt(s,'Jo',805,430,55,35,22,C.gray);txt(s,'Shembull: për t’u paguar 100, pranuar 60 ose 110.',66,620,1130,40,27,C.blue,true);

s=slide('Edhe shërbimet e tjera kanë gjurmë','Rekuizitat, trajnimet personale dhe rezervimet e fushave.','Burimi: InventoryService, IssueService, PersonalTrainingService, FieldService. Lidhjet janë specifike për secilin shërbim; rezervimi privat mund të mos ketë lojtar.');
[['Rekuizita','Dalje nga stoku','Arkëtim / detyrim'],['Trajnim personal','Lojtar dhe orar','Tarifë / pagesë'],['Fusha','Termin ose seri','Arkëtim / dëftesë']].forEach((r,i)=>{let y=245+i*125;let aa=card(s,r[0],64,y,275,85,C.navy,C.white),bb=card(s,r[1],440,y,335,85),cc=card(s,r[2],875,y,341,85);link(s,aa,bb);link(s,bb,cc);});

s=slide('Sponsorizimet, të ndara nga shpenzimet e klubit','Plani, kontrata dhe shpenzimi lidhen me kategorinë. Shembull ilustrues.','Burimi: SponsorshipService, SponsorshipsController. Qasje Super Admin. Shembull ilustrues nga kërkesa e përdoruesit: pagat 20000, mbuluar 2000, mbetur 18000. Nuk është rezultat real.');
flow(s,['Plani vjetor','Sponsor / kontratë','Buxheti i kategorisë','Faturë ose pagë'],235);
txt(s,'20 000',66,440,300,85,59,C.ink,true);txt(s,'Plani për paga',66,530,300,44,25,C.gray);txt(s,'2 000',480,440,300,85,59,C.green,true);txt(s,'Të mbuluara',480,530,300,44,25,C.gray);txt(s,'18 000',885,440,330,85,59,C.blue,true);txt(s,'Për t’u mbuluar',885,530,330,44,25,C.gray);txt(s,'Kontroll i afateve, faturave të dyfishta dhe pagës së të njëjtit muaj.',66,619,1145,42,24,C.ink);

s=slide('Trajneri lidhet me specialistin','Rasti mjekësor dhe menuja ndjekin rrjedha të veçanta.','Burimi: CareReferralsController, PlayerCareService, AttendanceService, CoachMenuService, NutritionMenuService. Menu AI kërkon konfigurim; drafti publikohet pas aprovimit. Kufizimi mjekësor Unavailable ndalon prezencën, nuk skadon automatikisht nga ReviewDate.');
let cr=card(s,'Trajneri\nhap rastin',64,325,235,110,C.navy,C.white),doc=card(s,'Mjeku\nshqyrton rastin',410,230,310,105),restr=card(s,'Udhëzime / pushim\nKontroll te prezenca',875,230,341,105),nut=card(s,'Menu / draft AI\nShqyrtim dhe aprovim',410,445,310,110),pub=card(s,'Menuja te prindi\nNjoftim për ndryshim',875,445,341,110);link(s,cr,doc);link(s,doc,restr);link(s,cr,nut);link(s,nut,pub);txt(s,'Vetëm informacioni i lejuar kalon te trajneri dhe prindi.',66,625,1150,38,25,C.gray);

s=slide('Prindi e ka klubin në telefon','','Burimi: ParentPortalController/Service, ParentAccessService, ParentProgressService. Foto AI ilustruese. Njoftimet kërkojnë konfigurimin dhe pëlqimin e pajisjes.');
image(s,photo('parent-player.png'),655,180,561,470);txt(s,'Një link për familjen',66,235,550,70,37,C.blue,true);txt(s,'Orari dhe trajneri\nPagesat dhe prezenca\nEcuria dhe menuja',66,335,530,200,32);txt(s,'Njoftime pas aktivizimit nga prindi',66,570,540,80,27,C.gray);

s=slide('Automatizimi mbështet punën e stafit','Ngjarja në sistem nis veprimin përkatës.','Burimi: MonthlyBillingWorker (cikël 15 minuta), ParentAutomationService, ParentPushWorker, NutritionDraftWorker, MenuNotificationService. Kujtesat nuk garantojnë lexim. Detyrimet nuk shlyhen vetëm nga shënimi i pagesës në pritje.');
rows(s,[['Faturimi mujor','Sistemi gjeneron detyrimet sipas tarifave.'],['Stërvitja','Kujtesë 1 orë para, kur njoftimet janë aktive.'],['Ndryshimi i orarit','Sistemi njofton abonentët përkatës.'],['Pagesa dhe menuja','Kujtesë në datën e caktuar dhe njoftim pas ndryshimit të menusë.']],225,100);

s=slide('Qasje sipas përgjegjësisë','Secili përdorues ka rolin dhe lojtarët që i takojnë.','Burimi: Models/ApplicationUser.cs Roles dhe Authorize në kontrollues. SuperAdmin mban edhe Admin; sponsorizimet kërkojnë SuperAdmin. Nuk ka rol të veçantë Menaxher. Prindi hyn me token; kjo nuk është llogari administrative.');
table(s,[['Roli','Fokusi'],['Super Admin / Admin','Sponsorizime sipas rolit, administrim dhe raporte'],['Operator / Shikues','Operim ose lexim sipas autorizimit'],['Trajner','Ekipet e caktuara dhe zhvillimi i lojtarëve'],['Mjek / Nutricist','Lojtarët e caktuar dhe kujdesi përkatës'],['Rezervime private / Prind','Fushat përkatëse / portali i familjes']],230,[400,752]);

s=slide('Drejtuesi sheh përdorimin e programit','Regjistrimet e trajnerëve, ditët aktive dhe aktiviteti i fundit.','Burimi: CoachActivityService, Views/Reports/CoachActivity. Screenshot i pamjes origjinale me modele sintetike. 1 regjistrim unik = 1 pikë. Numri i ekipeve/lojtarëve ndikon; nuk është matës i cilësisë së trajnerit.');
image(s,path.join(assets,'report-screenshot.png'),64,228,1152,350,'contain');txt(s,'1 regjistrim unik = 1 pikë',66,597,700,45,31,C.blue,true);txt(s,'Pamje reale e ndërfaqes me të dhëna ilustruese. Pikët matin përdorimin.',66,652,1130,30,19,C.gray);

s=slide('Përfitime që mund t’i vlerësoni në praktikë','Rezultati matet me proceset e klubit tuaj.','Këto janë përfitime të mundshme të arsyetuara nga funksionet; nuk janë rezultate të matura. Përdorni provën për të matur kohën e regjistrimit, sqarimet me prindin dhe kontrollet financiare.');
rows(s,[['Më pak kërkim','Informacioni i lojtarit lidhet me kartelën e tij.'],['Më shumë kontroll','Shuma reale, historiku dhe arsyet mbeten të gjurmueshme.'],['Komunikim më i qartë','Prindi e gjen informacionin e lejuar në portal.'],['Punë e dukshme','Drejtuesi kontrollon raportet sipas periudhës.']],225,100);

s=slide('Pse kjo platformë për klubin tuaj','Aftësi konkrete që punojnë së bashku.','Diferencues produkti të verifikuar nga kodi, jo pretendime ekskluziviteti ndaj konkurrentëve. Nuk ka studim tregu apo krahasim konkurrues në këtë prezantim.');
let choices=[['Puna sportive me financat','Kartela lidh prezencën, zhvillimin dhe pagesat.'],['Buxhetet e sponsorëve','Plan, shpenzim dhe raport në regjistër të ndarë.'],['Kujdesi lidhet me terrenin','Kufizimi i mjekut kontrollon shënimin e prezencës.'],['Identiteti i klubit tuaj','Emri dhe logoja përshtaten nga konfigurimi.']];choices.forEach((r,i)=>{let x=64+(i%2)*600,y=235+Math.floor(i/2)*190;txt(s,r[0],x,y,550,86,33,C.blue,true);txt(s,r[1],x,y+95,545,95,27);});

s=slide('Zbatimi dhe oferta për klubin','Propozim pune që përshtatet me nevojat tuaja.','Rend i propozuar, jo angazhim kontraktual. Nuk është verifikuar import automatik ose izolim multi-tenant. Konfirmoni vendosjen, migrimin, mbështetjen, çmimin dhe afatet para ofertës.');
flow(s,['Përcaktojmë\nmodulet','Konfigurojmë\nklubin / rolet','Provojmë\nme një ekip','Shtrijmë\npërdorimin'],240);
txt(s,'Oferta',66,440,300,55,36,C.blue,true);txt(s,'Çmimi: [për t’u përcaktuar]\nAfati dhe mbështetja: [për t’u dakorduar]',420,440,785,130,30);txt(s,'Vendosja dhe transferimi i të dhënave konfirmohen para fillimit.',66,625,1130,40,24,C.gray);

s=slide('','','Mbyllja: kërkoni takim për demonstrim të një rrjedhe të zgjedhur nga klubi. Plotësoni emrin, telefonin dhe emailin përpara dërgimit. Foto AI, përdorur përsëri si sfond i kapakut sipas identitetit të deck-ut.',true);
image(s,photo('hero-coach.png'),0,0,1280,720);txt(s,'Platforma në punën\ne klubit tuaj',64,165,690,180,62,C.white,true);txt(s,'Një ekip. Një rrjedhë reale.\nNjë demonstrim i qartë.',66,390,650,110,32,'#DFE7F1');txt(s,'Rezervoni prezantimin praktik',66,535,700,55,32,C.gold,true);txt(s,'[Emri / kompania]\n[Telefoni]   [Emaili]',66,605,770,76,25,C.white);

await (await PresentationFile.exportPptx(p)).save(path.join(build,'sales-draft.pptx'));
// PDF is exported by native PowerPoint after validation.
for(let i=0;i<p.slides.items.length;i++){const png=await p.export({slide:p.slides.items[i],format:'png',scale:1});await fs.writeFile(path.join(assets,'Slide_Previews',`slide-${String(i+1).padStart(2,'0')}.png`),new Uint8Array(await png.arrayBuffer()));}
console.log('Exported '+p.slides.items.length+' slides, draft PPTX, PDF and previews.');
