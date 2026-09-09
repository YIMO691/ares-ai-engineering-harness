// Read-only refresh: retain scroll position and opened evidence while observing.
const key = "ares-observer:" + location.pathname;
try {
 const saved = JSON.parse(sessionStorage.getItem(key) || "null");
 if (saved) { document.querySelectorAll("details").forEach((e,i)=>e.open=saved.open.includes(i)); scrollTo(0,saved.y); }
} catch {}
setTimeout(()=>{
 const open=[...document.querySelectorAll("details")].flatMap((e,i)=>e.open?[i]:[]);
 sessionStorage.setItem(key,JSON.stringify({y:scrollY,open}));
 if(!document.hidden && !getSelection()?.toString()) location.reload();
},10000);
