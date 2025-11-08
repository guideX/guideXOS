(function(){
  const $ = s=>document.querySelector(s);
  function token(){ return localStorage.getItem('gx_token') || ''; }
  function authHeaders(){ const t = token(); return t? {'Authorization':'Bearer '+t}:{}; }
  function show(el){ el.style.display = 'block'; }
  function hide(el){ el.style.display = 'none'; }
  function toggle(el){ el.style.display = (el.style.display==='block'?'none':'block'); }
  function drag(win){
    const tb = win.querySelector('.titlebar');
    let sx=0, sy=0, ox=0, oy=0, md=false;
    tb.addEventListener('mousedown',(e)=>{ md=true; sx=e.clientX; sy=e.clientY; const r=win.getBoundingClientRect(); ox=r.left; oy=r.top; });
    document.addEventListener('mousemove',(e)=>{ if(!md)return; const dx=e.clientX-sx, dy=e.clientY-sy; win.style.left=(ox+dx)+'px'; win.style.top=(oy+dy)+'px'; });
    document.addEventListener('mouseup',()=>md=false);
    const closeBtn = win.querySelector('[data-close]'); if(closeBtn) closeBtn.addEventListener('click',()=>hide(win));
  }
  async function apiList(path){
    const resp = await fetch(`/v1/fs/entries?path=${encodeURIComponent(path||'')}&recursive=false&pageSize=1000`, {headers:authHeaders()});
    if(resp.status===401){ location.href='/Account/Login'; return {items:[]}; }
    return await resp.json();
  }
  async function apiRead(path){
    const resp = await fetch(`/v1/fs/files/content?path=${encodeURIComponent(path)}`, {headers:authHeaders()});
    if(!resp.ok) return '';
    const ab = await resp.arrayBuffer();
    return new TextDecoder().decode(ab);
  }
  async function apiWrite(path, text){
    const body = new TextEncoder().encode(text);
    const resp = await fetch(`/v1/fs/files/content?path=${encodeURIComponent(path)}&createParents=true`,{method:'PUT',headers:Object.assign({'Content-Type':'application/octet-stream'},authHeaders()),body});
    return resp.ok;
  }
  function updateClock(){ const d=new Date(); $('#clock').textContent=d.toLocaleTimeString(); }
  function ensureAuth(){ if(!token()){ location.href='/Account/Login'; return false; } return true; }

  if(!ensureAuth()) return;

  // Taskbar/start
  $('#startBtn').addEventListener('click',()=>toggle($('#startMenu')));
  setInterval(updateClock, 1000); updateClock();
  $('#logoutBtn').addEventListener('click',()=>{ localStorage.removeItem('gx_token'); location.href='/Account/Login'; });

  // Windows
  const wNotepad = $('#win-notepad'); const wFiles = $('#win-files');
  drag(wNotepad); drag(wFiles);
  $('#startMenu .menu-item[data-app="notepad"]').addEventListener('click',()=>{ show(wNotepad); hide($('#startMenu')); });
  $('#startMenu .menu-item[data-app="files"]').addEventListener('click',()=>{ show(wFiles); hide($('#startMenu')); refreshFiles(); });

  // Desktop icons: list CloudFS root
  async function loadIcons(){
    const res = await apiList('/');
    const c = $('#desktopIcons'); c.innerHTML='';
    (res.items||[]).forEach(it=>{
      const d = document.createElement('div'); d.className='icon';
      d.innerHTML = `<img src="/img/file.png"><div>${it.name}</div>`;
      d.addEventListener('dblclick',()=>{
        if(it.type==='File'){ $('#np-path').value = it.path; show(wNotepad); openNotepad(); }
        else if(it.type==='Directory'){ $('#fm-path').value = it.path + '/'; show(wFiles); refreshFiles(); }
      });
      c.appendChild(d);
    });
  }

  // Notepad
  async function openNotepad(){ const p=$('#np-path').value; $('#np-text').value = await apiRead(p); }
  $('#np-open').addEventListener('click', openNotepad);
  $('#np-save').addEventListener('click', async ()=>{
    const p=$('#np-path').value; const t=$('#np-text').value; if(!p) return;
    const ok = await apiWrite(p,t); if(ok){ await refreshFiles(); }
  });

  // File Manager
  async function refreshFiles(){
    const p = $('#fm-path').value || '/';
    const res = await apiList(p);
    const ul = $('#fm-list'); ul.innerHTML='';
    (res.items||[]).forEach(it=>{
      const li=document.createElement('li'); li.textContent = `${it.type==='Directory'?'[DIR]':'[FILE]'} ${it.name}`;
      li.addEventListener('dblclick', async ()=>{
        if(it.type==='Directory'){ $('#fm-path').value = it.path + '/'; await refreshFiles(); }
        else { $('#np-path').value = it.path; show(wNotepad); await openNotepad(); }
      });
      ul.appendChild(li);
    });
  }
  $('#fm-refresh').addEventListener('click', refreshFiles);

  loadIcons();
})();
