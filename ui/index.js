import React from 'react';
import {floatingPosition} from './floating-position';
import railClassic from './icons/rail-classic.svg';
import railModern from './icons/rail-modern.svg';
import railOld from './icons/rail-old.svg';
import railSteel from './icons/rail-steel.svg';
import railWood from './icons/rail-wood.svg';
import styleClassic from './icons/style-classic.svg';
import styleModern from './icons/style-modern.svg';
import styleOld from './icons/style-old.svg';
import styleTunnel from './icons/style-tunnel.svg';
import styleSteel from './icons/style-steel.svg';
import styleWood from './icons/style-wood.svg';
import styleAuto from './icons/style-auto.svg';
import styleNone from './icons/style-none.svg';
import styleCurb from './icons/style-curb.svg';

import {bindValue,useValue,trigger} from 'cs2/api';

import {useLocalization} from 'cs2/l10n';

import {FloatingButton,Tooltip} from 'cs2/ui';

import icon from './stairs.svg';

import styles from './style.css';

// The game UI loader only loads the sibling CSS when this export is present.

export const hasCSS=true;

import closeIcon from './icons/close.svg';

import plusIcon from './icons/plus.svg';

import minusIcon from './icons/minus.svg';

import checkIcon from './icons/check.svg';

import moveIcon from './icons/move.svg';

import trashIcon from './icons/trash.svg';

import startIcon from './icons/start.svg';

import endIcon from './icons/end.svg';

import stairsIcon from './icons/stair-title.svg';

import infoIcon from './icons/info.svg';

import widthIcon from './icons/width.svg';
import resizeWidthIcon from './icons/resize-width.svg';
import editIcon from './icons/edit.svg';
import rotateLeftIcon from './icons/rotate-left.svg';
import rotateRightIcon from './icons/rotate-right.svg';
import overwriteIcon from './icons/overwrite.svg';
import zeroIcon from './icons/zero-circle.svg';
import copyIcon from './icons/copy.svg';
import libraryIcon from './icons/library.svg';
import saveTemplateIcon from './icons/save-template.svg';
import directionIcon from './icons/directions.svg';
import alignmentIcon from './icons/alignment.svg';

import heightIcon from './icons/height.svg';

import guidesIcon from './icons/guides.svg';

import rightAngleIcon from './icons/right-angle.svg';

import supportIcon from './icons/support.svg';
import surfaceIcon from './icons/surface.svg';
import networkIcon from './icons/network.svg';

const h=(type,props,...children)=>React.createElement(type,props&&props.className?{...props,className:props.className.split(' ').map(x=>styles[x]||x).join(' ')}:props,...children);

const defaults={floatingActions:'',templateLength:0,templateDate:'',templateTest:'',templates:'',canStoreTemplate:false,groupPlacement:false,widthHandle:'',open:false,haveA:false,ready:false,draft:false,canCommit:false,editing:false,width:2,point:0,pointCount:2,selectedOffset:0,moving:-1,count:0,offsetA:0,offsetB:0,status:'select',connection:'',guide:'',dimensions:'',snapDirections:true,snapAlignment:true,showSnapGuides:true,snapRadius:.75,heightStep:1,ramp:false,style:0,rightAngle:false,guideId:0,guideCount:0,hoverPoint:-1,selectedSupport:-1,hiddenSupports:0,canBranch:false,detailId:0,detailWidth:2,snapObjects:false,snapNetworks:false,absoluteHeight:false,worldHeight:0,groundHeight:0,gentle:false,solid:false,railing:-1,deckColor:-1,railColor:-1};

const bindings=Object.fromEntries(Object.entries(defaults).map(([k,v])=>[k,bindValue('StairBuilder',k,v)]));

const send=command=>trigger('StairBuilder','command',command);

function Menu(){const ready=useValue(bindings.ready),open=useValue(bindings.open);return ready?h(Tooltip,{tooltip:'Stair Builder'},h(FloatingButton,{src:icon,tinted:false,selected:open,onSelect:()=>send('toggle')})):null;}

const icons={saveTemplate:saveTemplateIcon,overwrite:overwriteIcon,zero:zeroIcon,edit:editIcon,rotateLeft:rotateLeftIcon,rotateRight:rotateRightIcon,copy:copyIcon,library:libraryIcon,directions:directionIcon,alignment:alignmentIcon,surface:surfaceIcon,network:networkIcon,guides:guidesIcon,close:closeIcon,plus:plusIcon,minus:minusIcon,check:checkIcon,move:moveIcon,trash:trashIcon,start:startIcon,end:endIcon,stairs:stairsIcon,info:infoIcon,width:widthIcon,height:heightIcon,rightAngle:rightAngleIcon,support:supportIcon};

const glyph=name=>h('img',{className:'icon',src:icons[name],alt:'','aria-hidden':true});

const styleIcons=[styleClassic,styleModern,styleOld,styleTunnel,styleSteel,styleWood];
const railIcons={'-1':styleAuto,0:styleNone,1:railClassic,2:railModern,3:railOld,4:railSteel,5:styleCurb,7:railWood};
const palette=[0xf3f1e7,0xaab2b6,0x414a52,0xe37852,0xe8bd62,0x78b99b,0x6db8e8,0xb699d1];
function ColorControl({label,value,command,t}){
 const format=v=>v<0?'':'#'+v.toString(16).padStart(6,'0').toUpperCase();
 const [text,setText]=React.useState(format(value));
 React.useEffect(()=>setText(format(value)),[value]);
 const valid=text===''||/^#?[0-9a-f]{6}$/i.test(text);
 return h('div',{className:'colorControl'},
  h('div',{className:'colorHeading'},h('span',null,label),h('button',{className:'textButton',disabled:value<0,onClick:()=>{setText('');send(command+':-1');},title:t('originalColor')},t('originalColor'))),
  h('div',{className:'swatches'},palette.map(rgb=>h('button',{key:rgb,className:'swatch'+(value===rgb?' chosen':''),style:{backgroundColor:format(rgb)},onClick:()=>send(command+':'+rgb),'aria-label':label+' '+format(rgb),'aria-pressed':value===rgb,title:format(rgb)},value===rgb?'✓':null))),
  h('div',{className:'colorEntry'},h('span',{className:'colorSample',style:{backgroundColor:value<0?'transparent':format(value)}}),h('input',{className:'hexInput'+(!valid?' invalid':''),key:value,defaultValue:format(value),placeholder:t('originalColor'),maxLength:7,'aria-label':label+' HEX','aria-invalid':!valid,title:valid?t('hexColor'):t('invalidColor'),onFocus:e=>e.target.select(),onInput:e=>setText(e.target.value),onBlur:e=>{const v=e.target.value.trim();if(v===''||/^#?[0-9a-f]{6}$/i.test(v))send(command+':'+(v===''?-1:parseInt(v.replace('#',''),16)));},onKeyDown:e=>{e.stopPropagation();if(e.key==='Enter'){e.preventDefault();e.target.blur();}if(e.key==='Escape'){e.preventDefault();e.target.value=format(value);setText(format(value));e.target.blur();}}}),h('small',null,valid?'HEX':t('invalidColor'))));
}

function TemplatePanel({s,t,saveOnly=false,onSaved}){
 const [query,setQuery]=React.useState(''),[name,setName]=React.useState(''),[rename,setRename]=React.useState(null),[confirm,setConfirm]=React.useState(null);
 const [pendingSave,setPendingSave]=React.useState(null),[pendingRename,setPendingRename]=React.useState(null);
 let data={items:[],limit:50,failed:false};try{if(s.templates)data=JSON.parse(s.templates);}catch{data.failed=true;}
 const styleName=t(['styleDefault','styleModern','styleOld','styleTunnel','styleSteel','styleWood'][s.style]||'styleDefault');
 const baseName=(styleName+'_'+Number(s.width).toFixed(1)+'m_'+t('templateLengthPrefix')+Number(s.templateLength).toFixed(1)+'m_'+s.templateDate).slice(0,40);
 let suggestedName=baseName;for(let n=2;data.items.some(x=>x.name.toLocaleLowerCase()===suggestedName.toLocaleLowerCase());n++){const suffix='_'+n;suggestedName=baseName.slice(0,40-suffix.length)+suffix;}
 const act=(action,id,name)=>send('library:'+JSON.stringify({action,id,name}));
 const saveTemplate=()=>{if(s.canStoreTemplate&&data.items.length<50&&!data.failed){const chosen=name.trim()||suggestedName;setPendingSave({name:chosen,count:data.items.length});act('save',null,chosen);}};
 const startRename=item=>setRename({id:item.id,name:item.name});
 const submitRename=()=>{if(rename?.name.trim()){setPendingRename({id:rename.id,name:rename.name.trim()});act('rename',rename.id,rename.name);}};
 const confirmAction=()=>{if(confirm){act(confirm.action,confirm.id);setConfirm(null);}};
 const request=(action,item)=>{if(action==='load')act(action,item.id);else setConfirm({action,id:item.id,name:item.name});};
 React.useEffect(()=>{if(pendingSave&&data.items.length>pendingSave.count&&data.items.some(x=>x.name===pendingSave.name)){setName('');setPendingSave(null);if(onSaved)onSaved();}if(pendingRename&&data.items.some(x=>x.id===pendingRename.id&&x.name===pendingRename.name)){setRename(null);setPendingRename(null);}},[s.templates,pendingSave,pendingRename]);
 const matches=data.items.filter(x=>x.name.toLocaleLowerCase().includes(query.toLocaleLowerCase()));
 const manageButton=(label,action,icon,disabled=false)=>h(Tooltip,{tooltip:label},h('button',{className:'templateIcon',disabled,onClick:action,'aria-label':label},glyph(icon)));
 const testSerial=React.useRef(0);
 React.useEffect(()=>{
  if(!s.templateTest)return;let r;try{r=JSON.parse(s.templateTest);}catch{return;}
  if(testSerial.current===r.serial)return;testSerial.current=r.serial;
  const item=data.items.find(x=>x.id===r.id);
  switch(r.action){
   case 'name':setName(r.name);break;case 'duplicateName':setName(r.duplicate||'');break;case 'blankName':setName('   ');break;
   case 'longName':setName(r.prefix+' '+('가'.repeat(32)));break;case 'nameLong':setName(r.prefix+' '+('나'.repeat(31)));break;
   case 'save':saveTemplate();break;case 'queryMatch':setQuery(r.prefix);break;case 'queryHangul':setQuery('한글');break;case 'queryEmpty':setQuery('NO_MATCH_없는검색');break;case 'queryClear':setQuery('');break;
   case 'rename':if(item)startRename(item);break;case 'renameName':if(rename)setRename({...rename,name:r.name+' 수정'});break;case 'renameSubmit':submitRename();break;case 'renameDuplicate':if(rename)setRename({...rename,name:r.duplicate});break;case 'renameCancel':setRename(null);break;
   case 'delete':if(item&&!data.failed)request('delete',item);break;case 'load':if(item)request('load',item);break;case 'overwrite':if(item&&s.canStoreTemplate&&!data.failed)request('overwrite',item);break;
   case 'confirm':confirmAction();break;case 'cancel':setConfirm(null);break;
  }
 },[s.templateTest]);
 React.useEffect(()=>{if(testSerial.current)trigger('StairBuilder','templateTestReport',JSON.stringify({serial:testSerial.current,query,name,suggestedName,rename,confirm,total:data.items.length,visible:data.items.filter(x=>x.name.toLocaleLowerCase().includes(query.toLocaleLowerCase())).map(x=>({id:x.id,name:x.name})),saveEnabled:s.canStoreTemplate&&data.items.length<50&&!data.failed}));},[s.templateTest,s.templates,s.canStoreTemplate,query,name,rename,confirm]);
 if(saveOnly)return h('div',{className:'templatePanel'},data.failed?h('div',{className:'error'},t('templateReadError')):null,s.canStoreTemplate?h(React.Fragment,null,h('div',{className:'templateLabel'},t('templateName')),h('div',{className:'templateForm'},h('div',{className:'templateNameEntry'},h('input',{value:name,maxLength:40,'aria-label':t('templateName'),onChange:e=>setName(e.target.value),onKeyDown:e=>e.stopPropagation()}),!name?h('span',{className:'templatePlaceholder'},suggestedName):null),h('button',{className:'button',disabled:data.items.length>=50||data.failed,onClick:saveTemplate},t('saveTemplate')))):h('div',{className:'templateEmpty'},t('templateSaveHint')),s.status.startsWith('template')?h('div',{className:'templateNotice'},t(s.status)):null,data.items.length>=50?h('div',{className:'templateEmpty'},t('templateFull')):null);
 return h('div',{className:'templatePanel'},
  h('div',{className:'libraryHeading'},h('strong',null,t('templates')),h('span',null,data.items.length+' / 50')),
  data.failed?h('div',{className:'error'},t('templateReadError')):null,
  s.status.startsWith('template')&&!data.failed?h('div',{className:'templateNotice'+(['templateNameExists','templateNameInvalid','templateError','templateFull','templateMissing'].includes(s.status)?' error':'')},t(s.status)):null,

  data.items.length>=50?h('div',{className:'templateEmpty'},t('templateFull')):null,
  h('div',{className:'templateLabel'},t('searchTemplates')),
  h('input',{className:'templateSearch',value:query,placeholder:t('searchTemplates'),'aria-label':t('searchTemplates'),onChange:e=>setQuery(e.target.value),onKeyDown:e=>e.stopPropagation()}),
  confirm?h('div',{className:'templateConfirm'},h('span',null,t(confirm.action==='overwrite'?'overwriteTemplateConfirm':'deleteTemplateConfirm')+' '+confirm.name),h('div',{className:'actions'},h('button',{className:'button primary',onClick:confirmAction},t('confirm')),h('button',{className:'button',onClick:()=>setConfirm(null)},t('cancel')))):null,
  matches.length===0?h('div',{className:'templateEmpty'},t(data.items.length===0?'templateEmpty':'templateNoResults')):null,
  h('div',{className:'templateList'},matches.map(item=>h('div',{key:item.id,className:'templateRow'},
   rename?.id===item.id?h('div',{className:'templateForm'},h('input',{value:rename.name,maxLength:40,'aria-label':t('renameTemplate'),onChange:e=>setRename({...rename,name:e.target.value}),onKeyDown:e=>e.stopPropagation()}),h('button',{className:'button',disabled:!rename.name.trim(),onClick:submitRename},t('confirm')),h('button',{className:'button iconButton',onClick:()=>setRename(null),'aria-label':t('cancel')},glyph('close'))):h(React.Fragment,null,
    h('button',{className:'templateName',onClick:()=>request('load',item),title:item.name+' · '+t('placeTemplate')},h('span',null,item.name),h('small',null,Number(item.width).toFixed(1)+' m · '+item.points+' '+t('pointCountUnit'))),
    manageButton(t('renameTemplate'),()=>startRename(item),'edit'),
    manageButton(t('overwriteTemplate'),()=>request('overwrite',item),'overwrite',!s.canStoreTemplate||data.failed),
    manageButton(t('deleteTemplate'),()=>request('delete',item),'trash',data.failed))))));
}

function useViewport(){
 const read=()=>[window.innerWidth,window.innerHeight,parseFloat(window.getComputedStyle(document.documentElement).fontSize)||1];
 const [viewport,setViewport]=React.useState(read);
 React.useEffect(()=>{
  const update=()=>setViewport(old=>{const next=read();return old.every((v,i)=>v===next[i])?old:next;});
  window.addEventListener('resize',update);
  const observer=typeof MutationObserver==='function'?new MutationObserver(update):null;
  observer?.observe(document.documentElement,{attributes:true,attributeFilter:['style','class']});
  return ()=>{window.removeEventListener('resize',update);observer?.disconnect();};
 },[]);
 return viewport;
}

function App(){

 const s={};for(const k of Object.keys(bindings))s[k]=useValue(bindings[k]);

 const viewport=useViewport();
 const [tab,setTab]=React.useState('layout');
 const [libraryOpen,setLibraryOpen]=React.useState(false);
 const [saveOpen,setSaveOpen]=React.useState(false);
 React.useEffect(()=>{if(s.groupPlacement||s.moving>=0||(!s.open&&!s.detailId))setSaveOpen(false);},[s.groupPlacement,s.moving,s.open,s.detailId]);
 React.useEffect(()=>{if(s.templateTest){try{setLibraryOpen(JSON.parse(s.templateTest).action!=='close');}catch{}}},[s.templateTest]);
 React.useEffect(()=>{if(s.groupPlacement)setLibraryOpen(false);},[s.groupPlacement]);
 React.useEffect(()=>{if(!s.draft)setTab('layout');},[s.draft]);
 React.useEffect(()=>send('view:'+tab),[tab,s.draft]);
 const {translate}=useLocalization();const t=k=>translate('StairBuilder.'+k,k);

 const button=(label,command,iconName,extra='',disabled=false)=>h('button',{className:'button '+extra,disabled,onClick:()=>send(command),'aria-label':label,title:label},iconName?glyph(iconName):null,label?h('span',null,label):null);

 const iconButton=(label,command,name,disabled=false)=>h('button',{className:'button iconButton',disabled,onClick:()=>send(command),'aria-label':label,title:label},glyph(name));

 const anchor=s.floatingActions.split(',').map(Number);
 const actionPosition=floatingPosition(anchor,viewport,saveOpen);
 const floating=actionPosition?h('div',{className:'objectActions',style:actionPosition},
  h('div',{className:'objectActionBar'},
   (s.open?s.editing:s.detailId>0)?h(Tooltip,{tooltip:t('copy')},h('button',{className:'objectAction',onClick:()=>{setSaveOpen(false);send(s.open?'copy':'detailCopy');},'aria-label':t('copy')},glyph('copy'))):null,
   h(Tooltip,{tooltip:t('storeTemplate')},h('button',{className:'objectAction'+(saveOpen?' selected':''),onClick:()=>{if(!s.open)send('detailEdit');setLibraryOpen(false);setSaveOpen(!saveOpen);},'aria-label':t('storeTemplate'),'aria-expanded':saveOpen},glyph('saveTemplate'))),
   h(Tooltip,{tooltip:t('moveWhole')},h('button',{className:'objectAction',onClick:()=>{setSaveOpen(false);send(s.open?'moveWhole':'detailMove');},'aria-label':t('moveWhole')},glyph('move')))),
  saveOpen&&s.canStoreTemplate?h('div',{className:'objectSave',onKeyDownCapture:e=>{if(e.key==='Escape'){e.stopPropagation();e.preventDefault();setSaveOpen(false);}}},h('div',{className:'objectSaveHeading'},h('strong',null,t('storeTemplate')),h('button',{className:'objectAction',onClick:()=>setSaveOpen(false),'aria-label':t('cancel')},glyph('close'))),h(TemplatePanel,{s,t,saveOnly:true,onSaved:()=>setSaveOpen(false)})):null):null;
 if(!s.open)return s.detailId>0?h(React.Fragment,null,floating,h('div',{className:'panel detailPanel'},

  h('div',{className:'header'},h('div',{className:'title'},glyph('stairs'),h('strong',null,'Stair Builder')),iconButton(t('cancel'),'detailClose','close')),

  h('div',{className:'body'},h('div',{className:'sectionTitle'},glyph('width'),t('width')+' / '+Number(s.detailWidth).toFixed(1)+' m'),h('div',{className:'pointActions'},button(t('edit'),'detailEdit','move','primary'),button(t('delete'),'detailDelete','trash','danger')))

 )):null;

 const widthHandle=s.widthHandle?s.widthHandle.split(',').map(Number):[];
 const points=s.guide.split(',').map(Number);

 const metrics=(s.dimensions.match(/[0-9]+(?:\.[0-9]+)?/g)||[]).map(Number);

 const bad=['assets','error','missing','prefabs','templateError','templateReadError','templateFull','templateNameExists','templateNameInvalid'].includes(s.status);

 return h(React.Fragment,null,floating,
  widthHandle.length===5&&widthHandle.every(Number.isFinite)?h('div',{className:'widthHandle'+(widthHandle[4]?' dragging':''),style:{left:widthHandle[0]+'%',top:widthHandle[1]+'%'}},h('img',{src:resizeWidthIcon,alt:'',style:{transform:'rotate('+widthHandle[2]+'deg)'}}),h('span',null,widthHandle[3].toFixed(1)+' m')):null,

  s.guideCount>0&&points.length===s.guideCount*2&&points.every(Number.isFinite)?h('div',{className:'markers'},(s.haveA||!s.draft?Array.from({length:s.guideCount},(_,i)=>i):[0]).map(i=>h('button',{

   key:s.guideId+':'+i,

   className:'marker'+(s.draft&&s.selectedSupport<0&&s.point===i?' active':'')+(s.hoverPoint===i?' hovered':'')+(s.moving===i?' movingMarker':''),

   disabled:s.moving>=0,style:{left:points[i*2]+'%',top:points[i*2+1]+'%'},

   title:t('movePointHint'),'aria-label':t('waypoint')+' '+(i+1),

   onMouseDown:event=>{if(event.button===0)send('dragPoint:'+s.guideId+':'+i);},
   onClick:()=>send('selectPoint:'+s.guideId+':'+i),

   onMouseEnter:()=>send('hoverPoint:'+i),onMouseLeave:()=>send('hoverPoint:-1')

  },i===0?glyph('start'):i===s.guideCount-1?glyph('end'):h('span',null,i+1),s.moving===i?h('span',{className:'markerLabel'},t('stateMoving')):null))):null,

  h('div',{className:'panel'},

   h('div',{className:'header'},h('div',{className:'title'},glyph('stairs'),h('strong',null,'Stair Builder')),iconButton(t('cancel'),'toggle','close')),

   s.draft?h('div',{className:'tabs',role:'tablist'},['layout','appearance'].map(key=>h('button',{key,role:'tab','aria-selected':tab===key,className:'tab'+(tab===key?' selected':''),onClick:()=>{setTab(key);setLibraryOpen(false);}},glyph(key==='layout'?'height':key==='appearance'?'stairs':'guides'),t(key)))):null,

   h('div',{className:'body'},

    h('div',{className:'libraryToolbar'},!s.draft?button(t('new'),'new','plus','primary'):null,h('button',{className:'button'+(libraryOpen?' selected':''),onClick:()=>setLibraryOpen(!libraryOpen),'aria-expanded':libraryOpen},glyph('library'),t('templates'))),
    libraryOpen?h(TemplatePanel,{s,t}):null,
    s.groupPlacement?h('div',{className:'pointActions'},button(t('rotateLeft'),'rotateLeft','rotateLeft','quiet'),button(t('rotateRight'),'rotateRight','rotateRight','quiet')):null,

    (!libraryOpen&&(!s.draft||tab==='layout'))?h(React.Fragment,null,
    h('div',{className:'sectionTitle'},glyph('width'),t('width')+' · '+Number(s.width).toFixed(1)+' m'),

    h('div',{className:'segments'},[1,2,3,4,6,8].map(w=>h('button',{key:w,className:'segment'+(s.width===w?' selected':''),'aria-pressed':s.width===w,onClick:()=>send('width:'+w)},w+' m'))),

    s.draft?h('div',{className:'segments'},[false,true].map(value=>h('button',{key:String(value),className:'segment'+(s.ramp===value?' selected':''),'aria-pressed':s.ramp===value,onClick:()=>{if(s.ramp!==value)send('ramp');}},t(value?'ramp':'stairs')))):null,


    s.draft?h(React.Fragment,null,

     h('div',{className:'points'},Array.from({length:Math.ceil(s.pointCount/2)},(_,row)=>h('div',{className:'pointRow',key:row},[row*2,row*2+1].filter(i=>i<s.pointCount).map(i=>h('button',{key:i,className:'point'+(s.point===i?' selected':''),disabled:s.moving>=0,onClick:()=>send('point:'+i)},i===0?glyph('start'):i===s.pointCount-1?glyph('end'):glyph('move'),h('span',null,i===0?t('startPoint'):i===s.pointCount-1?t('endPoint'):t('waypoint')+' '+(i+1))))))),

     [['absoluteElevation','worldHeight','absoluteValue'],['relativeElevation','groundHeight','relativeValue']].map(([label,key,command])=>h('div',{key},
      h('div',{className:'sectionTitle heightLabel'},h('span',{className:'heightLabelText'},glyph('height'),t(label)),key==='groundHeight'?h('button',{className:'zeroHeight',onClick:()=>send('relativeValue:0'),title:t('groundZero'),'aria-label':t('groundZero')},glyph('zero')):null),
      h('div',{className:'heightControl'},iconButton(t('lower'),command+':'+(s[key]-s.heightStep),'minus'),
       h('span',{className:'value'},h('input',{key:s.point+':'+s[key],type:'number',className:'heightInput',defaultValue:Number(s[key].toFixed(2)),step:s.heightStep,'aria-label':t(label),onFocus:e=>e.target.select(),onBlur:e=>{const v=e.target.value.trim();if(v!==''&&Number.isFinite(Number(v)))send(command+':'+Number(v));else e.target.value=s[key];},onKeyDown:e=>{e.stopPropagation();if(e.key==='Enter'){e.preventDefault();e.target.blur();}if(e.key==='Escape'){e.preventDefault();e.target.value=s[key];e.target.blur();}}}),h('small',null,' m')),
       iconButton(t('raise'),command+':'+(s[key]+s.heightStep),'plus')))),
     h('div',{className:'snapRadius'},h('span',null,t('heightStep')),h('div',{className:'segments'},[1,.5,.25].map(step=>h('button',{key:step,'aria-label':t('heightStep')+' '+step+' m','aria-pressed':s.heightStep===step,className:'segment'+(s.heightStep===step?' selected':''),onClick:()=>send('heightStep:'+step)},step+' m')))),

     s.moving<0&&s.point>0?button(t('level'),'level','width','quiet wide'):null,

     s.moving<0?h('div',{className:'pointActions'},button(t('move'),'move','move','quiet'),s.editing?button(t('branch'),'branch','plus','quiet',!s.canBranch):null):null,

     s.editing&&s.moving<0&&!s.canBranch?h('div',{className:'bridgeHint'},t('branchApplyFirst')):null,

     h('div',{className:'routeActions'},button(t('addPoint'),'addPoint','plus','quiet',s.moving>=0||s.pointCount>=32),s.pointCount>2?iconButton(t('undoPoint'),'undoPoint','minus',s.moving>=0):null),

     s.selectedSupport>=0?h('div',{className:'supportSelection'},button(t('deleteSupport'),'deleteSupport','trash','danger wide',s.moving>=0)):null,

     s.hiddenSupports>0?button(t('restoreSupports')+' ('+s.hiddenSupports+')','restoreSupports','support','quiet wide',s.moving>=0):null,

     s.gentle?h('div',{className:'gentleWarning'},glyph('info'),t('gentleWarning')):s.ramp?h('div',{className:'bridgeHint'},t('rampInfo')):metrics.length===3&&Number(metrics[0])===0?h('div',{className:'bridgeHint'},t('flat')):metrics.length===3?h('div',{className:'metrics'},[ ['stepCount',metrics[0]],['stepRise',(metrics[1]*100).toFixed(1)+' cm'],['stepTread',(metrics[2]*100).toFixed(1)+' cm'] ].map(([key,value])=>h('div',{key,className:'metric'},h('small',null,t(key)),h('strong',null,value)))):null

    ):null,

    ):null,

    !libraryOpen&&s.draft&&tab==='appearance'?h(React.Fragment,null,
     h('div',{className:'sectionTitle'},glyph('stairs'),t('stairStyle')),
    s.draft?h('div',{className:'styleGrid'},['styleDefault','styleModern','styleOld','styleTunnel','styleSteel','styleWood'].map((key,i)=>h('button',{key,className:'styleCard'+(s.style===i?' selected':''),'aria-pressed':s.style===i,title:t(key),onClick:()=>send('style:'+i)},h('img',{src:styleIcons[i],alt:'','aria-hidden':true}),h('span',null,t(key))))):null,
     h('div',{className:'sectionTitle'},glyph('support'),t('foundation')),
     h('div',{className:'segments'},[[false,'piers'],[true,'solid']].map(([value,label])=>h('button',{key:label,className:'segment'+(s.solid===value?' selected':''),onClick:()=>{if(s.solid!==value)send('solid');}},t(label)))),
     h('div',{className:'sectionTitle'},glyph('guides'),t('railingStyle')),
     h('div',{className:'railChoices styleGrid'},[[-1,'styleAuto'],[0,'railNone'],[1,'styleDefault'],[2,'styleModern'],[3,'styleOld'],[4,'styleSteel'],[5,'railCurb'],[7,'styleWood']].map(([value,label])=>h('button',{key:value,className:'railCard'+(s.railing===value?' selected':''),'aria-pressed':s.railing===value,onClick:()=>send('railing:'+value),title:t(label)},h('img',{src:railIcons[value],alt:'','aria-hidden':true}),h('span',null,t(label))))),
     h(ColorControl,{label:t('deckColor'),value:s.deckColor,command:'deckColor',t}),
     h(ColorControl,{label:t('railColor'),value:s.railColor,command:'railColor',t})
    ):null,
    !libraryOpen?h('div',{className:'status'+(bad?' error':'')},glyph('info'),h('span',null,t(s.status))):null,

    s.draft&&tab==='layout'&&s.connection?h('div',{className:'connection'},t(s.connection)):null

   ),

   s.draft&&!libraryOpen?h('div',{className:'footer'},
    h('div',{className:'snapDock'},h('span',{className:'snapLabel'},t('snapping')),['snapObjects','snapNetworks','snapDirections','snapAlignment','rightAngle','showSnapGuides'].map((key,i)=>h(Tooltip,{key,tooltip:t(key)},h('button',{className:'snapIcon'+(s[key]?' enabled':''),onClick:()=>send(key),'aria-label':t(key),'aria-pressed':s[key]},glyph(['surface','network','directions','alignment','rightAngle','guides'][i])))),h('div',{className:'snapStepper',title:t('snapRadius')},h('button',{disabled:s.snapRadius<=.25,onClick:()=>send('snapRadius:'+Math.max(.25,Math.round((s.snapRadius-.25)*100)/100)),'aria-label':t('snapRadius')+' '+t('lower')},glyph('minus')),h('span',null,Number(s.snapRadius.toFixed(2))+' m'),h('button',{disabled:s.snapRadius>=4,onClick:()=>send('snapRadius:'+Math.min(4,Math.round((s.snapRadius+.25)*100)/100)),'aria-label':t('snapRadius')+' '+t('raise')},glyph('plus')))),

    h('div',{className:'actions'},button(t(s.editing?'apply':'create'),'commit','check','primary',!s.canCommit),iconButton(t('cancel'),'cancel','close'),s.editing?iconButton(t('delete'),'delete','trash'):null)

   ):null

  )

 );

}

export default registry=>{registry.append('GameTopRight',Menu);registry.append('Game',App);};



