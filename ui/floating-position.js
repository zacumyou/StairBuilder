// Gameface cannot evaluate mixed percentage/rem calc() expressions.
// Resolve units before passing a single pixel value to the UI engine.
export function floatingPosition(anchor, viewport, saveOpen) {
 const [width,height,rem]=viewport;
 if(anchor.length!==2||!anchor.every(Number.isFinite)||![width,height,rem].every(v=>Number.isFinite(v)&&v>0))return null;
 const clamp=(value,min,max)=>Math.max(min,Math.min(Math.max(min,max),value));
 const left=clamp(anchor[0]*width/100,Math.min(170*rem,width/2),width-410*rem);
 const top=clamp(anchor[1]*height/100,Math.min(100*rem,height/2),height-(saveOpen?290:120)*rem);
 return {left:Math.round(left)+'px',top:Math.round(top)+'px'};
}
