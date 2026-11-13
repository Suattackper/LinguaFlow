import { spawn } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

const positional = process.argv.slice(2).filter(arg => !arg.startsWith('--'));
const base = positional[0] || 'http://127.0.0.1:5193/';
const output = positional[1] || 'artifacts/phase01-browser';
const chrome = process.env.CHROME_BIN || 'C:/Program Files/Google/Chrome/Application/chrome.exe';
if (!existsSync(chrome)) throw new Error('Chrome unavailable');
mkdirSync(output, { recursive: true });
const profile = mkdtempSync(join(tmpdir(), 'linguaflow-phase01-'));
const child = spawn(chrome, ['--headless=new','--no-sandbox','--disable-gpu','--no-first-run',`--user-data-dir=${profile}`,'--remote-debugging-port=0',base], { stdio:'ignore' });
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
async function until(fn, ms=30000) { const end=Date.now()+ms; while(Date.now()<end) { const result=await Promise.resolve().then(fn).catch(()=>null); if(result) return result; await sleep(250); } throw new Error('Browser timed out'); }
let ws;
try {
  const port = await until(() => { const p=join(profile,'DevToolsActivePort'); return existsSync(p) && readFileSync(p,'utf8').split('\n')[0]; });
  const target = await until(async () => (await(await fetch(`http://127.0.0.1:${port}/json`)).json()).find(x=>x.type==='page')?.webSocketDebuggerUrl);
  ws = new WebSocket(target);
  await new Promise((resolve,reject)=>{ws.onopen=resolve;ws.onerror=reject});
  let next=1; const pending=new Map(); const errors=[];
  ws.onmessage = event => { const message=JSON.parse(event.data); if(message.method==='Runtime.exceptionThrown') errors.push(message.params); if(!pending.has(message.id)) return; const entry=pending.get(message.id);pending.delete(message.id);message.error?entry.reject(new Error(message.error.message)):entry.resolve(message.result); };
  const send=(method,params={})=>new Promise((resolve,reject)=>{const id=next++;pending.set(id,{resolve,reject});ws.send(JSON.stringify({id,method,params}))});
  const evaluate=async expression => (await send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true})).result?.value;
  await send('Runtime.enable'); await send('Page.enable');
  await send('Page.addScriptToEvaluateOnNewDocument',{source:`window.__linguaVitals={cls:0,lcp:null,shifts:[]};try{new PerformanceObserver(list=>{for(const entry of list.getEntries())if(!entry.hadRecentInput){window.__linguaVitals.cls+=entry.value;window.__linguaVitals.shifts.push({value:entry.value,sources:(entry.sources||[]).map(source=>({tag:source.node?.tagName,className:String(source.node?.className||'').slice(0,70),previous:source.previousRect,current:source.currentRect}))})}}).observe({type:'layout-shift',buffered:true});new PerformanceObserver(list=>{const entries=list.getEntries();if(entries.length)window.__linguaVitals.lcp=entries.at(-1).startTime}).observe({type:'largest-contentful-paint',buffered:true})}catch{}`});
  await until(()=>evaluate("!!document.querySelector('.topbar') && !document.querySelector('.boot-loading')"));
  const rows=[];
  async function visit(path,width,theme,lang,marker) {
    await evaluate(`localStorage.setItem('linguaflow.theme',${JSON.stringify(theme)});localStorage.setItem('linguaflow.language',${JSON.stringify(lang)})`);
    await send('Emulation.setDeviceMetricsOverride',{width,height:950,deviceScaleFactor:1,mobile:width<700});
    await send('Page.navigate',{url:new URL(path,base).href});
    await until(()=>evaluate(`!!document.querySelector(${JSON.stringify(marker)}) && !document.querySelector('.boot-loading') && !document.querySelector('.state-message.loading')`));
    const result=await evaluate("({viewport:innerWidth,scroll:document.documentElement.scrollWidth,theme:document.documentElement.dataset.theme,lang:document.documentElement.lang,error:getComputedStyle(document.querySelector('#blazor-error-ui')).display!=='none',markdown:document.querySelectorAll('.markdown-content').length,strong:document.querySelectorAll('.markdown-content strong').length,code:document.querySelectorAll('.markdown-content code').length,unsafe:document.querySelectorAll('.markdown-content script,.markdown-content iframe,.markdown-content a[href^=\"javascript:\"]').length,filePicker:document.querySelector('.file-picker-button')?.innerText||null,checklist:document.querySelectorAll('.assessment-checklist li').length,badChecklistLinks:[...document.querySelectorAll('.assessment-checklist button')].filter(x=>!x.textContent.trim()).length})");
    rows.push({path,width,theme,lang,...result});
    if(result.scroll>width+1 || result.error || result.theme!==theme || result.lang!==lang || result.unsafe) throw new Error(`Layout/render failed: ${JSON.stringify(rows.at(-1))}`);
    return result;
  }
  const course='courses/english-life-office-it/';
  if(!process.argv.includes('--journey-only')) {
  for(const [theme,lang] of [['light','vi-VN'],['light','en-US'],['dark','vi-VN'],['dark','en-US']]) {
    for(const width of [360,390,768,1024,1366,1920]) {
      const result=await visit(`${course}units/unit-01`,width,theme,lang,'.reader-main');
      if(result.markdown<20 || result.strong<1 || result.code<1) throw new Error(`Markdown missing at ${width}/${theme}/${lang}`);
      if(process.argv.includes('--phase02') && (result.checklist<6 || result.badChecklistLinks)) throw new Error(`Assessment checklist missing at ${width}/${theme}/${lang}`);
      console.log('unit-01',width,theme,lang,result.markdown,result.strong,result.code);
      if(width===390 && lang==='vi-VN') { const shot=await send('Page.captureScreenshot',{format:'png'});writeFileSync(join(output,`unit-01-${theme}-mobile.png`),Buffer.from(shot.data,'base64')); }
      if(process.argv.includes('--capture-unit01-matrix') && theme==='light' && lang==='vi-VN' && [360,390,768,1024,1366].includes(width)) { const shot=await send('Page.captureScreenshot',{format:'png'});writeFileSync(join(output,`unit-01-${width}-${theme}-${lang}.png`),Buffer.from(shot.data,'base64')); }
    }
  }
  for(const id of ['unit-18','unit-36']) for(const width of [390,1366]) { const result=await visit(`${course}units/${id}`,width,'dark','vi-VN','.reader-main');if(result.markdown<1) throw new Error(`${id} has no rendered markdown`); console.log(id,width,result.markdown); }
  for(const [path,marker] of [[`${course}practice?unit=unit-01`,'.practice-wizard'],[`${course}practice/import`,'.localized-file-picker'],[`${course}backup`,'.localized-file-picker'],[`${course}goals`,'.app-select'],[`${course}workspace`,'.app-select']]) {
    for(const [theme,lang,width] of [['light','vi-VN',390],['dark','en-US',1366]]) {
      const result=await visit(path,width,theme,lang,marker);
      if(path.endsWith('/import') || path.endsWith('/backup')) { if(!result.filePicker || result.filePicker==='Choose File') throw new Error(`File picker not localized: ${path}`); }
      if(path.includes('practice?') && result.markdown<1) throw new Error('Practice Markdown missing');
      console.log(path,width,theme,lang);
    }
  }
  }
  if(process.argv.includes('--phase02')) {
    const assessment=JSON.parse(readFileSync('src/LinguaFlow.Web/wwwroot/Courses/English/english-life-office-it/Assessments/assessments.json','utf8'));
    const auto=assessment.answers.find(x=>x.assessmentMode==='auto-choice' && x.unitId==='unit-01');
    const manual=assessment.answers.find(x=>x.assessmentMode==='manual-review' && x.unitId==='unit-01' && x.exerciseId.includes('-c3-'));
    if(!auto || !manual) throw new Error('Representative source exercises missing');
    await visit(`${course}units/unit-01`,390,'light','vi-VN','.reader-main');
    if(await evaluate("document.querySelector('.reader-main')?.innerText.includes('ANSWER_KEYS/')")) throw new Error('Internal answer-key path is visible to learners');
    const autoId=JSON.stringify(auto.exerciseId), manualId=JSON.stringify(manual.exerciseId);
    const selected=await evaluate(`(() => { const root=document.getElementById(${autoId}), option=[...root.querySelectorAll('input[type=radio]')].find(x=>x.value.startsWith(${JSON.stringify(auto.answer+'.')})); if(!option)return false;option.click();return true; })()`);
    if(!selected) throw new Error('Auto-choice option unavailable');
    await until(()=>evaluate(`!document.getElementById(${autoId}).querySelector('.exercise-submit').disabled`));
    await evaluate(`document.getElementById(${autoId}).querySelector('.exercise-submit').click()`);
    await until(()=>evaluate(`!!document.getElementById(${autoId}).querySelector('.exercise-feedback')`));
    const automatic=await evaluate(`document.getElementById(${autoId}).querySelector('.exercise-feedback').innerText`);
    if(!automatic.includes('Điểm tự động') || !automatic.includes('Đúng')) throw new Error(`Automatic feedback unclear: ${automatic}`);
    await evaluate(`(() => { const input=document.getElementById(${manualId}).querySelector('textarea');const setter=Object.getOwnPropertyDescriptor(HTMLTextAreaElement.prototype,'value').set;setter.call(input,${JSON.stringify(manual.answer)});input.dispatchEvent(new Event('input',{bubbles:true})); })()`);
    await until(()=>evaluate(`!document.getElementById(${manualId}).querySelector('.exercise-submit').disabled`));
    await evaluate(`document.getElementById(${manualId}).querySelector('.exercise-submit').click()`);
    await until(()=>evaluate(`!!document.getElementById(${manualId}).querySelector('.exercise-feedback')`));
    const selfReview=await evaluate(`document.getElementById(${manualId}).querySelector('.exercise-feedback').innerText`);
    if(!selfReview.includes('Đã nộp') || !selfReview.includes(manual.answer)) throw new Error(`Manual feedback unclear: ${selfReview}`);
    const screenshot=await send('Page.captureScreenshot',{format:'png'});writeFileSync(join(output,'phase02-feedback-mobile.png'),Buffer.from(screenshot.data,'base64'));
    await send('Page.navigate',{url:new URL(`${course}units/unit-01`,base).href});
    await until(()=>evaluate(`!!document.getElementById(${autoId})?.querySelector('.exercise-feedback') && !!document.getElementById(${manualId})?.querySelector('.exercise-feedback')`));
    const restored=await evaluate(`({auto:document.getElementById(${autoId}).querySelector('.exercise-feedback').innerText,manual:document.getElementById(${manualId}).querySelector('.exercise-feedback').innerText})`);
    if(!restored.auto.includes('Điểm tự động') || !restored.manual.includes('Đã nộp')) throw new Error('Attempt feedback not restored after reload');
    await send('Page.navigate',{url:new URL(`${course}units/unit-01#c1`,base).href});
    await until(()=>evaluate("document.querySelector('.journey-visible')?.id==='c1'"));
    const c1Id='unit-01-c1-06';
    const twoSlots=await evaluate(`(() => {const inputs=[...document.getElementById(${JSON.stringify(c1Id)}).querySelectorAll('.answer-blanks input')];for(const [index,value] of ['Does','join'].entries()){const setter=Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set;setter.call(inputs[index],value);inputs[index].dispatchEvent(new Event('input',{bubbles:true}));}return inputs.length})()`);
    if(twoSlots!==2) throw new Error(`C1 #6 does not have two answer slots: ${twoSlots}`);
    await until(()=>evaluate(`!document.getElementById(${JSON.stringify(c1Id)}).querySelector('.exercise-submit').disabled`));
    await evaluate(`document.getElementById(${JSON.stringify(c1Id)}).querySelector('.exercise-submit').click()`);
    await until(()=>evaluate(`document.getElementById(${JSON.stringify(c1Id)}).querySelector('.exercise-feedback')?.innerText.includes('Đúng')`));
    await send('Page.navigate',{url:new URL(`${course}units/unit-01#h`,base).href});
    await until(()=>evaluate("document.querySelector('.journey-visible')?.id==='h' && !!document.querySelector('.unit-test-info')"));
    await until(()=>evaluate("window.scrollY<560 || getComputedStyle(document.querySelector('.back-to-top')).visibility==='visible'"));
    const summaryShot=await send('Page.captureScreenshot',{format:'png'});writeFileSync(join(output,'unit-01-summary-mobile.png'),Buffer.from(summaryShot.data,'base64'));
    console.log('phase02 interaction: automatic, self-review and refresh passed');
  }
  if(process.argv.includes('--phase03')) {
    await visit(`${course}units/unit-01`,390,'light','vi-VN','.reader-main');
    const ruleWidth=await evaluate("document.querySelector('.assessment-rule-text')?.getBoundingClientRect().width");
    if(ruleWidth < 150) throw new Error(`Assessment checklist text too narrow: ${ruleWidth}`);
    const mobileShot=await send('Page.captureScreenshot',{format:'png'});writeFileSync(join(output,'phase03-journey-mobile.png'),Buffer.from(mobileShot.data,'base64'));
    const visible=()=>evaluate("[...document.querySelectorAll('.lesson-section')].filter(x=>getComputedStyle(x).display!=='none').map(x=>x.id)");
    if((await visible()).join()!=='intro') throw new Error('Initial journey section is not singular');
    await evaluate("document.querySelectorAll('.journey-stage')[1].click()");
    await until(async()=> (await visible()).join()==='c' && (await evaluate('location.hash'))==='#c');
    await evaluate("document.querySelector('#c .next-section').click()");
    await until(async()=> (await visible()).join()==='c1' && (await evaluate('location.hash'))==='#c1');
    const draftId=await evaluate("document.querySelector('#c1 textarea.answer-input')?.id");
    if(!draftId) throw new Error('Practice draft input missing');
    await evaluate("(() => {const input=document.querySelector('#c1 textarea.answer-input');input.value='Phase 03 retained draft';input.dispatchEvent(new Event('input',{bubbles:true}));})()");
    await evaluate("document.querySelectorAll('.journey-stage')[3].click()");
    await until(async()=> (await visible()).join()==='i' && (await evaluate('location.hash'))==='#i');
    await evaluate('history.back()');
    await until(async()=> (await visible()).join()==='c1' && (await evaluate('location.hash'))==='#c1');
    if(await evaluate("document.querySelector('#c1 textarea.answer-input').value") !== 'Phase 03 retained draft') throw new Error('Draft lost on back navigation');
    await send('Page.navigate',{url:new URL(`${course}units/unit-01#c1`,base).href});
    await until(async()=> (await visible()).join()==='c1');
    await until(()=>evaluate("document.querySelector('#c1 textarea.answer-input')?.value==='Phase 03 retained draft'"));
    await evaluate("document.querySelector('.reader-toolbar button').click()");
    await until(()=>evaluate("!!document.querySelector('.reader-focus') && !!document.querySelector('#c1.journey-visible')"));
    await evaluate("document.querySelector('.reader-toolbar button').click()");
    await visit(`${course}units/unit-18#g3`,390,'dark','en-US','.reader-main');
    await until(async()=> (await visible()).join()==='g3');
    await visit(`${course}units/unit-36#h`,1366,'light','vi-VN','.reader-main');
    await until(async()=> (await visible()).join()==='h');
    const shot=await send('Page.captureScreenshot',{format:'png'});writeFileSync(join(output,'phase03-deeplink-desktop.png'),Buffer.from(shot.data,'base64'));
    console.log('phase03 interaction: stages, deep links, back, draft, focus passed');
  }
  if(process.argv.includes('--phase04')) {
    for(const [theme,lang] of [['light','vi-VN'],['light','en-US'],['dark','vi-VN'],['dark','en-US']])
      for(const width of [360,390,768,1024,1366,1920]) {
        if(process.argv.includes('--phase04-quick') && (width!==390 || theme!=='light' || lang!=='vi-VN')) continue;
        await visit('',width,theme,lang,'.hero');
        const primary=await evaluate("document.querySelectorAll('.hero-actions .button.primary').length");
        if(primary!==1) throw new Error(`Home primary action missing: ${width}/${theme}/${lang}`);
        if(width===390 && lang==='vi-VN') {const shot=await send('Page.captureScreenshot',{format:'png'});writeFileSync(join(output,`phase04-home-${theme}-mobile.png`),Buffer.from(shot.data,'base64'));}
        console.log('home',width,theme,lang);
      }
    for(const [theme,lang] of [['light','vi-VN'],['light','en-US'],['dark','vi-VN'],['dark','en-US']])
      for(const width of [360,390,768,1024,1366,1920]) {
        if(process.argv.includes('--phase04-quick') && (width!==390 || theme!=='light' || lang!=='vi-VN')) continue;
        await visit('courses/english-life-office-it',width,theme,lang,'.course-resume');
        if(!(await evaluate("!!document.querySelector('.course-resume .button.primary')"))) throw new Error(`Course resume action missing: ${width}/${theme}/${lang}`);
        if(width===390 && lang==='vi-VN') {const shot=await send('Page.captureScreenshot',{format:'png'});writeFileSync(join(output,`phase04-course-${theme}-mobile.png`),Buffer.from(shot.data,'base64'));}
        console.log('course',width,theme,lang);
      }
    for(const [theme,lang,width] of [['light','vi-VN',390],['dark','en-US',390],['light','en-US',1366],['dark','vi-VN',1366]]) {
      if(process.argv.includes('--phase04-quick') && (width!==390 || theme!=='light' || lang!=='vi-VN')) continue;
      await visit(`${course}roadmap`,width,theme,lang,'.roadmap-group');
      const count=await evaluate("document.querySelectorAll('.roadmap-card').length");
      if(count!==36) throw new Error(`Roadmap has ${count} Units`);
      if(width===390 && lang==='vi-VN') {const shot=await send('Page.captureScreenshot',{format:'png'});writeFileSync(join(output,'phase04-roadmap-mobile.png'),Buffer.from(shot.data,'base64'));}
      console.log('roadmap',width,theme,lang,count);
    }
    const manifest=JSON.parse(readFileSync('src/LinguaFlow.Web/wwwroot/Courses/English/english-life-office-it/manifest.json','utf8'));
    for(const unit of [manifest.units[0],manifest.units[Math.floor(manifest.units.length/2)],manifest.units.at(-1)])
      for(const [width,theme,lang] of [[390,'light','vi-VN'],[1366,'dark','en-US']]) {
        if(process.argv.includes('--phase04-quick') && (unit.unitId!==manifest.units[Math.floor(manifest.units.length/2)].unitId || width!==390)) continue;
        await visit(`${course}vocabulary?unit=${unit.unitId}`,width,theme,lang,'.vocab-scope');
        const result=await evaluate("({visible:[...document.querySelectorAll('.vocabulary-tile')].map(x=>x.dataset.unitId),scope:document.querySelector('.vocab-scope')?.innerText,advanced:document.querySelector('.vocab-advanced')?.open,cta:document.querySelectorAll('.vocab-primary-actions a').length})");
        if(!result.visible.length || result.visible.some(x=>x!==unit.unitId) || result.advanced || result.cta!==2 || !result.scope.includes(unit.title)) throw new Error(`Vocabulary Unit filter failed: ${unit.unitId} ${JSON.stringify(result)}`);
        if(width===390 && lang==='vi-VN') {const shot=await send('Page.captureScreenshot',{format:'png'});writeFileSync(join(output,'phase04-vocabulary-mobile.png'),Buffer.from(shot.data,'base64'));}
        console.log('vocabulary',unit.unitId,width,theme,lang,result.visible.length);
      }
    if(process.argv.includes('--phase04-quick')) {
      const selected=manifest.units[Math.floor(manifest.units.length/2)].unitId;
      const vocabularyUrl=`${course}vocabulary?unit=${selected}`;
      const before=await evaluate("document.querySelector('.vocabulary-tile .vocab-status + button')?.innerText");
      await evaluate("document.querySelector('.vocabulary-tile .vocab-word-top button').click();document.querySelector('.vocabulary-tile .vocab-status + button').click()");
      await until(()=>evaluate("document.querySelector('.vocabulary-tile .vocab-word-top button')?.getAttribute('aria-pressed')?.toLowerCase()==='true'"));
      await until(async()=> (await evaluate("document.querySelector('.vocabulary-tile .vocab-status + button')?.innerText"))!==before);
      await visit(vocabularyUrl,390,'light','vi-VN','.vocab-scope');
      const retained=await evaluate("({favorite:document.querySelector('.vocabulary-tile .vocab-word-top button')?.getAttribute('aria-pressed'),queue:document.querySelector('.vocabulary-tile .vocab-status + button')?.innerText})");
      if(retained.favorite?.toLowerCase()!=='true' || retained.queue===before) throw new Error(`Vocabulary state lost after refresh: ${JSON.stringify(retained)}`);
      console.log('vocabulary favorite and queue retained after refresh');
    }
    await visit(`${course}units/${manifest.units[Math.floor(manifest.units.length/2)].unitId}#g3`,390,'light','vi-VN','.reader-main');
    await visit('',390,'light','vi-VN','.home-resume');
    const href=await evaluate("document.querySelector('.home-resume .button.primary')?.getAttribute('href')");
    if(!href?.includes(`${manifest.units[Math.floor(manifest.units.length/2)].unitId}#g3`)) throw new Error(`Home resume position lost: ${href}`);
    console.log('home resume',href);
  }
  if(process.argv.includes('--phase05')) {
    const manifest=JSON.parse(readFileSync('src/LinguaFlow.Web/wwwroot/Courses/English/english-life-office-it/manifest.json','utf8'));
    for(const [width,theme,lang] of [[360,'light','vi-VN'],[390,'dark','en-US'],[768,'light','en-US'],[1024,'dark','vi-VN'],[1366,'light','vi-VN'],[1920,'dark','en-US']]) {
      for(const unit of [manifest.units[0],manifest.units[Math.floor(manifest.units.length/2)],manifest.units.at(-1)]) {
        if(process.argv.includes('--phase05-quick') && (width!==390 || unit.unitId!==manifest.units[0].unitId)) continue;
        await visit(`${course}practice?unit=${unit.unitId}`,width,theme,lang,'.practice-steps');
        const practice=await evaluate("({steps:document.querySelectorAll('.practice-steps button').length,active:document.querySelectorAll('.practice-steps .is-active').length,selected:document.querySelector('.practice-wizard .app-select-value')?.innerText,create:[...document.querySelectorAll('.practice-wizard button')].some(x=>x.innerText.includes('tập')||x.innerText.includes('session'))})");
        if(practice.steps!==4 || practice.active!==1 || !practice.selected?.includes(unit.title)) throw new Error(`Practice selection failed ${unit.unitId}: ${JSON.stringify(practice)}`);
        await visit(`${course}flashcards?mode=new&unit=${unit.unitId}`,width,theme,lang,'.flashcard-session');
        const deck=await evaluate("({count:document.querySelector('.flashcard-top')?.innerText,progress:!!document.querySelector('.flashcard-progress'),error:!!document.querySelector('.content-warning[role=alert]')})");
        if(!deck.progress || deck.error) throw new Error(`Flashcard deck failed ${unit.unitId}: ${JSON.stringify(deck)}`);
        console.log('phase05',unit.unitId,width,theme,lang);
      }
    }
    await visit(`${course}practice/import`,390,'light','vi-VN','.localized-file-picker');
    const picker=await evaluate("document.querySelector('.file-picker-button')?.innerText");
    if(!picker || picker==='Choose File') throw new Error(`Import picker not localized: ${picker}`);
    const shot=await send('Page.captureScreenshot',{format:'png'});writeFileSync(join(output,'phase05-import-mobile.png'),Buffer.from(shot.data,'base64'));
    await evaluate("(() => {const input=document.querySelector('.practice-wizard textarea');input.value='{invalid';input.dispatchEvent(new Event('input',{bubbles:true}));document.querySelector('.practice-wizard .button.course-action').click()})()");
    await until(()=>evaluate("!!document.querySelector('.practice-wizard [role=alert]')"));
    if(await evaluate("!!document.querySelector('.practice-criteria')")) throw new Error('Invalid JSON unexpectedly produced an import preview');
    await visit(`${course}practice?unit=${manifest.units[0].unitId}`,390,'light','vi-VN','.practice-steps');
    await evaluate("document.querySelector('.practice-wizard .button.course-action').click()");
    await until(()=>evaluate("!!document.querySelector('.practice-steps button:nth-child(2).is-active')"));
    const request=await evaluate("({preview:!!document.querySelector('.practice-preview'),session:document.querySelector('.practice-wizard')?.innerText.includes('Tải Markdown')})");
    if(!request.preview) throw new Error(`Practice session export step failed: ${JSON.stringify(request)}`);
    await visit(`${course}flashcards?mode=new&unit=${manifest.units[0].unitId}`,390,'light','vi-VN','.flashcard-session');
    const before=await evaluate("(async()=>{const db=await new Promise((resolve,reject)=>{const r=indexedDB.open('linguaflow-learning',2);r.onsuccess=()=>resolve(r.result);r.onerror=()=>reject(r.error)});return await new Promise((resolve,reject)=>{const r=db.transaction('records','readonly').objectStore('records').index('courseId').getAll('english-life-office-it');r.onsuccess=()=>resolve(r.result.length);r.onerror=()=>reject(r.error)})})()");
    await evaluate("document.querySelector('.flashcard-actions .button').click()");
    await evaluate("document.querySelector('.flashcard-actions .button').click()");
    await until(()=>evaluate("document.querySelector('.flashcard-top')?.innerText.includes('2 /')"));
    await evaluate("[...document.querySelectorAll('.flashcard-actions button')].find(x=>x.innerText.includes('Hoàn tác')).click()");
    try { await until(()=>evaluate("document.querySelector('.flashcard-top')?.innerText.includes('1 /')"),5000); }
    catch { throw new Error(`Flashcard undo failed: ${JSON.stringify(await evaluate("({top:document.querySelector('.flashcard-top')?.innerText,buttons:[...document.querySelectorAll('.flashcard-actions button')].map(x=>x.innerText),warning:document.querySelector('.content-warning')?.innerText})"))}`); }
    const after=await evaluate("(async()=>{const db=await new Promise((resolve,reject)=>{const r=indexedDB.open('linguaflow-learning',2);r.onsuccess=()=>resolve(r.result);r.onerror=()=>reject(r.error)});return await new Promise((resolve,reject)=>{const r=db.transaction('records','readonly').objectStore('records').index('courseId').getAll('english-life-office-it');r.onsuccess=()=>resolve(r.result.length);r.onerror=()=>reject(r.error)})})()");
    if(after!==before) throw new Error(`Flashcard undo did not restore record count: ${before} -> ${after}`);
    console.log('phase05 practice session and flashcard undo passed');
  }
  if(process.argv.includes('--phase06')) {
    const manifest=JSON.parse(readFileSync('src/LinguaFlow.Web/wwwroot/Courses/English/english-life-office-it/manifest.json','utf8'));
    if(process.argv.includes('--mobile-unit-capture')) {
      await visit('',320,'light','vi-VN','.hero,.home-resume');
      for(const width of [320,390,768,1366]) for(const [theme,lang] of [['light','vi-VN'],['light','en-US'],['dark','vi-VN'],['dark','en-US']]) {
        await visit(`${course}units/${manifest.units[0].unitId}`,width,theme,lang,'.reader-main');
        const clicked=await evaluate("(() => { const link=[...document.querySelectorAll('.reader-sidebar .section-link')].find(x=>x.textContent.includes('Vocabulary'));link?.click();return {found:!!link,links:[...document.querySelectorAll('.reader-sidebar .section-link')].slice(0,5).map(x=>x.textContent),cards:document.querySelectorAll('.lesson-vocabulary-card').length} })()");
        if(!clicked.found) throw new Error(`Vocabulary section link missing: ${JSON.stringify(clicked)}`);
        await until(()=>evaluate("document.querySelector('.journey-visible')?.id==='a'"),5000).catch(async()=>{throw new Error(`Vocabulary navigation failed: ${JSON.stringify(await evaluate("({visible:document.querySelector('.journey-visible')?.id,hash:location.hash,cards:document.querySelectorAll('.lesson-vocabulary-card').length})"))}`)});
        const layout=await evaluate("({cards:[...document.querySelectorAll('.lesson-vocabulary-card')].filter(x=>getComputedStyle(x).display!=='none').length,cardList:document.querySelector('.lesson-vocabulary-cards') && getComputedStyle(document.querySelector('.lesson-vocabulary-cards')).display,table:document.querySelector('.vocabulary-scroll') && getComputedStyle(document.querySelector('.vocabulary-scroll')).display,scroll:document.documentElement.scrollWidth})");
        if(width<=390 && (layout.cards<1 || layout.table!=='none')) throw new Error(`Mobile lesson vocabulary failed: ${width} ${theme} ${lang} ${JSON.stringify(layout)}`);
        if(width>=768 && layout.cardList!=='none') throw new Error(`Desktop lesson table failed: ${width} ${theme} ${lang} ${JSON.stringify(layout)}`);
        const shot=await send('Page.captureScreenshot',{format:'png'});
        writeFileSync(join(output,`mobile-unit-vocabulary-${width}-${theme}-${lang}.png`),Buffer.from(shot.data,'base64'));
      }
      console.log('16 lesson vocabulary layouts and screenshots passed');
    }
    const unitRows=[];
    for(const reference of process.argv.includes('--phase06-pages-only') ? [] : manifest.units) {
      const started=performance.now();
      await visit(`${course}units/${reference.unitId}`,390,'light','vi-VN','.reader-main');
      const check=await evaluate("(() => {const ids=[...document.querySelectorAll('[id]')].map(x=>x.id);return {sections:document.querySelectorAll('.lesson-section').length,visible:[...document.querySelectorAll('.lesson-section')].filter(x=>getComputedStyle(x).display!=='none').length,heading:document.querySelector('.reader-main .journey-visible h2')?.innerText,fragment:location.hash,mainCount:document.querySelectorAll('main').length,exercise:document.querySelectorAll('.exercise-card,.interactive-exercise').length,assessment:!!document.querySelector('.assessment-overview'),body:document.querySelector('.reader-main')?.innerText.length||0,duplicateIds:ids.filter((id,i)=>ids.indexOf(id)!==i).slice(0,6)}})()");
      if(check.sections<1 || check.visible!==1 || check.body<100 || !check.assessment || check.mainCount!==1 || check.duplicateIds.length) throw new Error(`Unit render failed ${reference.unitId}: ${JSON.stringify(check)}`);
      await evaluate("document.querySelector('.journey-visible .next-section')?.click()");
      await until(()=>evaluate("document.querySelectorAll('.journey-visible').length===1 && location.hash.length>1"));
      const navigated=await evaluate("({section:document.querySelector('.journey-visible')?.id,hash:location.hash})");
      if(navigated.hash!==`#${navigated.section}`) throw new Error(`Section navigation failed ${reference.unitId}: ${JSON.stringify(navigated)}`);
      if([manifest.units[0].unitId,manifest.units[Math.floor(manifest.units.length/2)].unitId,manifest.units.at(-1).unitId].includes(reference.unitId)) { const shot=await send('Page.captureScreenshot',{format:'png'});writeFileSync(join(output,`phase06-${reference.unitId}-mobile.png`),Buffer.from(shot.data,'base64')); }
      unitRows.push({unitId:reference.unitId,loadMs:Math.round(performance.now()-started),...check,navigated});
      console.log('phase06 unit',reference.unitId,check.sections,unitRows.at(-1).loadMs);
    }
    const pageRows=[];
    const routes=[['','.hero,.home-resume'],[`${course.slice(0,-1)}`,'.course-detail'],[`${course}roadmap`,'.roadmap-card'],[`${course}vocabulary?unit=${manifest.units[0].unitId}`,'.vocabulary-tile'],[`${course}flashcards?mode=new&unit=${manifest.units[0].unitId}`,'.flashcard-session'],[`${course}practice?unit=${manifest.units[0].unitId}`,'.practice-steps'],[`${course}practice/import`,'.localized-file-picker'],['settings','.settings-card'],['help','.help-article']];
    const combinations=[['light','vi-VN'],['light','en-US'],['dark','vi-VN'],['dark','en-US']];
    for(const width of process.argv.includes('--phase06-units-only') ? [] : process.argv.includes('--mobile-plan') ? [320,390,768,1366] : [360,390,768,1024,1366,1920]) for(const [theme,lang] of combinations) {
      if(process.argv.includes('--phase06-page-quick') && (width!==(process.argv.includes('--help-only') ? 1366 : process.argv.includes('--phase06-capture') ? 390 : 360) || theme!==(process.argv.includes('--phase06-dark') ? 'dark' : 'light') || lang!==(process.argv.includes('--help-only') || process.argv.includes('--phase06-dark') ? 'en-US' : 'vi-VN'))) continue;
      const targets=process.argv.includes('--help-only') ? routes.filter(([path])=>path==='help') : process.argv.includes('--phase06-baseline') ? routes.filter((_,index)=>index%3===0) : process.argv.includes('--phase06-targeted') ? routes.filter((_,index)=>[0,4].includes(index)) : routes;
      for(const [path,marker] of targets) {
        const started=performance.now();
        await visit(path,width,theme,lang,marker);
        const result=await evaluate("(() => {const style=getComputedStyle(document.documentElement),art=document.querySelector('.hero-art');return {headings:[...document.querySelectorAll('#main h1')].map(x=>x.innerText),landmarks:document.querySelectorAll('main').length,focusables:[...document.querySelectorAll('#main button,#main a[href],#main input,#main textarea')].filter(x=>x.getClientRects().length>0).length,heroArt:art?{loaded:art.complete&&art.naturalWidth>0,rect:art.getBoundingClientRect().toJSON(),display:getComputedStyle(art).display,opacity:getComputedStyle(art).opacity,visibility:getComputedStyle(art).visibility}:null,fcp:performance.getEntriesByType('paint').find(x=>x.name==='first-contentful-paint')?.startTime||null,vitals:window.__linguaVitals||null,tokens:Object.fromEntries(['--text','--muted','--primary','--surface','--bg'].map(key=>[key,style.getPropertyValue(key).trim()]))}})()");
        if(result.headings.length!==1 || result.landmarks!==1) throw new Error(`Heading/landmark failed ${path} ${width} ${theme} ${lang}: ${JSON.stringify(result)}`);
        if((width===390 && theme==='light' && lang==='vi-VN' && ['','settings',`${course}practice/import`].includes(path)) || (process.argv.includes('--mobile-plan') && ['','settings',`${course}vocabulary?unit=${manifest.units[0].unitId}`,`${course}flashcards?mode=new&unit=${manifest.units[0].unitId}`].includes(path))) { if(path==='') await until(()=>evaluate("!document.querySelector('.hero-art') || (document.querySelector('.hero-art').complete && document.querySelector('.hero-art').naturalWidth>0)")); const shot=await send('Page.captureScreenshot',{format:'png'});writeFileSync(join(output,`mobile-${path ? path.split('/').at(-1).replace(/[^a-z0-9-]/gi,'-') : 'home'}-${width}-${theme}-${lang}.png`),Buffer.from(shot.data,'base64')); }
        pageRows.push({path,width,theme,lang,loadMs:Math.round(performance.now()-started),...result});
      }
      console.log('phase06 pages',width,theme,lang,targets.length);
    }
    await send('Emulation.setEmulatedMedia',{features:[{name:'prefers-reduced-motion',value:'reduce'}]});
    await visit(`${course}units/${manifest.units[0].unitId}`,390,'light','vi-VN','.reader-main');
    const reduced=await evaluate("({motion:matchMedia('(prefers-reduced-motion: reduce)').matches,scroll:getComputedStyle(document.documentElement).scrollBehavior,sections:document.querySelectorAll('.journey-visible').length,animation:getComputedStyle(document.querySelector('.lesson-section')).animationDuration})");
    if(!reduced.motion || reduced.scroll!=='auto' || reduced.sections!==1) throw new Error(`Reduced motion failed: ${JSON.stringify(reduced)}`);
    await send('Emulation.setEmulatedMedia',{features:[]});
    await visit('',390,'light','vi-VN','.hero,.home-resume');
    await evaluate("document.querySelector('.mobile-menu').click()");
    await until(()=>evaluate("!!document.querySelector('.mobile-drawer') && !!document.querySelector('.drawer-close')"));
    const drawerLayer=await evaluate("(() => {const drawer=document.querySelector('.mobile-drawer'),bar=document.querySelector('.topbar'),box=drawer.getBoundingClientRect();return {drawerZ:+getComputedStyle(drawer).zIndex,barZ:+getComputedStyle(bar).zIndex,top:box.top,hit:document.elementFromPoint(Math.min(100,box.right-10),Math.max(30,box.top+30))?.closest('.mobile-drawer')===drawer}})()");
    if(drawerLayer.drawerZ<=drawerLayer.barZ || drawerLayer.top!==0 || !drawerLayer.hit) throw new Error(`Mobile drawer layer failed: ${JSON.stringify(drawerLayer)}`);
    await evaluate("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true,cancelable:true}))");
    await until(()=>evaluate("!document.querySelector('.mobile-drawer')"));
    await evaluate("document.querySelector('.mobile-menu').click()");
    await until(()=>evaluate("!!document.querySelector('.drawer-backdrop')"));
    await evaluate("document.querySelector('.drawer-backdrop').click()");
    await until(()=>evaluate("!document.querySelector('.mobile-drawer') && !!document.querySelector('.topbar .language-trigger')"));
    console.log('mobile drawer layering, Escape and backdrop passed');
    await evaluate("document.querySelector('.quick-search-trigger').click()");
    await until(()=>evaluate("document.activeElement?.classList.contains('command-search')"));
    await evaluate("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Tab',shiftKey:true,bubbles:true,cancelable:true}))");
    const wrapped=await evaluate("document.activeElement?.closest('.command-overlay')!==null && !document.activeElement?.classList.contains('command-search')");
    if(!wrapped) throw new Error('Quick Search did not trap reverse Tab');
    await evaluate("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true,cancelable:true}))");
    await until(()=>evaluate("!document.querySelector('.command-overlay') && document.activeElement?.classList.contains('quick-search-trigger')"));
    await evaluate("document.querySelector('.topbar .language-trigger').click()");
    await until(()=>evaluate("!!document.querySelector('.topbar .language-menu') && !!document.activeElement?.classList.contains('language-option')"));
    const languageMovement=await evaluate("(() => {const before=document.activeElement?.textContent;const menu=document.querySelector('.topbar .language-menu');const options=[...menu.querySelectorAll('.language-option')];const index=options.indexOf(document.activeElement);const event=new KeyboardEvent('keydown',{key:'ArrowDown',bubbles:true,cancelable:true});document.activeElement?.dispatchEvent(event);return {before,after:document.activeElement?.textContent,prevented:event.defaultPrevented,index,options:options.map(x=>({text:x.textContent,disabled:x.disabled,tabIndex:x.tabIndex,rect:x.getBoundingClientRect().width}))}})()");
    await until(()=>evaluate("document.activeElement?.textContent?.includes('English EN')"),3000).catch(()=>{throw new Error(`Language arrow navigation did not move focus: ${JSON.stringify(languageMovement)}`)});
    await evaluate("document.activeElement?.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true,cancelable:true}))");
    await until(()=>evaluate("!document.querySelector('.topbar .language-menu') && document.activeElement?.classList.contains('language-trigger')"));
    await visit(`${course}units/${manifest.units[0].unitId}`,390,'light','vi-VN','.reader-main');
    await evaluate("window.scrollTo(0, document.body.scrollHeight)");
    await until(()=>evaluate("getComputedStyle(document.querySelector('.back-to-top')).visibility==='visible'"),5000).catch(async()=>{throw new Error(`Back to top did not appear: ${JSON.stringify(await evaluate("({y:scrollY,height:document.body.scrollHeight,viewport:innerHeight,button:!!document.querySelector('.back-to-top'),state:document.documentElement.dataset.backToTop})"))}`)});
    await until(()=>evaluate("document.querySelector('.back-to-top').getBoundingClientRect().bottom <= document.querySelector('.footer').getBoundingClientRect().top - 8"));
    await evaluate("document.querySelector('.back-to-top').click()");
    await until(()=>evaluate("window.scrollY < 50"),5000);
    console.log('phase06 keyboard dialogs and reduced motion passed');
    const phase06Name=process.argv.includes('--phase06-baseline')?'phase06-baseline.json':process.argv.includes('--phase06-units-only')?'phase06-units.json':process.argv.includes('--mobile-plan')?'mobile-plan.json':process.argv.includes('--phase06-page-quick')?'phase06-quick.json':'phase06-results.json';
    writeFileSync(join(output,phase06Name),JSON.stringify({unitRows,pageRows,exceptions:errors.length},null,2));
    console.log(`phase06 ${unitRows.length} Units and ${pageRows.length} page states passed`);
  }
  writeFileSync(join(output,'audit.json'),JSON.stringify({rows,exceptions:errors.length},null,2));
  if(errors.length) throw new Error(`${errors.length} browser exceptions`);
  console.log(`${rows.length} browser states passed`);
} finally {
  ws?.close();child.kill();
  try { rmSync(profile,{recursive:true,force:true,maxRetries:5,retryDelay:200}); }
  catch (error) {
    if (!['EBUSY','EPERM'].includes(error.code)) throw error;
    console.warn(`Temporary Chrome profile remains locked by Windows: ${profile}`);
  }
}
