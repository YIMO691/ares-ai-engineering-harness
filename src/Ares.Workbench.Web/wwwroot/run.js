(() => {
  const root=document.getElementById('run-view'); if(!root)return;
  let lastDiff='',busy=false; const rendered={};
  const text=(id,value)=>{document.getElementById(id).textContent=value??'';};
  const names={grounding:'Grounding',plan:'Plan',codex:'Primary Codex',test:'Test',review:'Reviewer',human:'Approval',deliver:'Deliver'};
  async function update(){
    if(busy)return;busy=true;
    try{
      const response=await fetch(location.pathname+'?handler=Snapshot',{cache:'no-store'});
      if(!response.ok)throw new Error('HTTP '+response.status);
      const d=await response.json();
      for(const id of ['state','current','rework','lifecycle','workflow'])text(id,d[id]);
      const acceptanceLink=document.getElementById("acceptance-link");if(acceptanceLink)acceptanceLink.hidden=!d.awaitingAcceptance;
      const cp=d.checkpoint;
      text('primary-status',d.primaryStatus);text('reviewer-status',d.reviewerStatus);
      text('invocations',cp?.codexInvocations??'历史记录');
      text('native-reuse',cp?((cp.nativeReuses??0)>0?'Yes · '+cp.nativeReuses+' 次':'No · 尚未复用'):'旧版 · 未记录');
      text('session-counts',cp?'Logical sessions: Primary '+(cp.primaryInvocations>0?1:0)+' / Reviewer '+(cp.reviewerInvocations>0?1:0)+
        ' · Invocations: Primary '+cp.primaryInvocations+' / Reviewer '+cp.reviewerInvocations+
        ' · Primary ID: '+(cp.primarySessionRef??'等待原生 ID')+' · Reviewer ID: '+(cp.reviewerSessionRef??'等待原生 ID'):'v0.1 历史记录保留，不能 Resume');
      for(const action of ['stop','cancel','resume'])document.getElementById(action+'-form').hidden=!d.controls[action];
      document.getElementById('blocked-card').hidden=!['Blocked','Paused'].includes(d.rawState);
      for(const [id,value] of [['blocked-at',d.blockedAt],['blocked-reason',d.failure],['blocked-category',d.blockedCategory],
        ['owner-fix',d.ownerFix],['resume-target',d.controls.resume?d.resumeTarget:'不可继续 · New Run']])text(id,value);
      text('failure',d.failure);document.getElementById('failure').hidden=!d.failure;
      text('poll-status','最近更新 '+new Date().toLocaleTimeString()+' · 每 2 秒更新');
      const fast=d.workflow.startsWith('FAST'),critical=d.workflow.startsWith('CRITICAL');
      const skipped=id=>fast&&['grounding','plan','review'].includes(id)||id==='human'&&!critical;
      for(const element of document.querySelectorAll('[data-step]')){
        const id=element.dataset.step,last=d.nodes.filter(n=>n.id===id||(n.id==='prepare'&&['grounding','plan'].includes(id))).at(-1);
        const active=(d.currentNode===id||d.currentNode==='prepare'&&['grounding','plan'].includes(id))&&['Running','Waiting'].includes(d.rawState);
        element.className=skipped(id)?'skipped':active?'active':last?.outcome==='Succeeded'?'done':last?'attention':'';
        element.textContent=names[id]+(skipped(id)?' · skipped':'');
      }
      const list=document.getElementById('timeline');list.replaceChildren();
      const eventIds=new Set();
      for(const event of d.timeline){
        if(eventIds.has(event.id))continue;eventIds.add(event.id);
        const li=document.createElement('li'),small=document.createElement('small'),title=document.createElement('strong'),detail=document.createElement('p');
        li.dataset.eventId=event.id;
        small.textContent=event.time;title.textContent=event.node+' · '+event.type;detail.textContent=event.detail.slice(0,700);
        li.append(small,title,detail);list.append(li);
      }
      for(const id of ['grounding','plan','codex','test','review','human']){
        const box=document.getElementById('output-'+id);if(!box)continue;
        const nodes=d.nodes.filter(n=>n.id===id);
        if(['grounding','plan'].includes(id)&&d[id]){
          const prepare=d.nodes.filter(n=>n.id==='prepare'&&n.outcome==='Succeeded').at(-1);
          if(prepare)nodes.push({...prepare,output:d[id],failure:null});
        }
        const signature=JSON.stringify([nodes,d.current,skipped(id),id==='human'?d.approvals:[]]);
        if(rendered[id]===signature)continue;rendered[id]=signature;
        box.replaceChildren();
        if(!nodes.length){const p=document.createElement('p');p.className='muted';p.textContent=skipped(id)?'此工作流跳过该步骤':(d.currentNode===id||d.currentNode==='prepare'&&['grounding','plan'].includes(id))?'当前步骤正在执行，完成后显示结果…':'等待执行';box.append(p);}
        for(const node of nodes){
          const details=document.createElement('details'),summary=document.createElement('summary'),pre=document.createElement('pre');
          details.open=node===nodes.at(-1);summary.textContent='第 '+node.attempt+' 次 · '+node.outcome;
          pre.textContent=node.failure||node.output||'执行完成';details.append(summary,pre);box.append(details);
        }
        if(id==='human')for(const approval of d.approvals){const p=document.createElement('p');p.textContent=approval.status+' · '+approval.comment;box.append(p);}
      }
      document.getElementById('approval').hidden=!d.pending;
      if(d.pending){document.getElementById('RequestId').value=d.pending.requestId;document.getElementById('RunVersion').value=d.pending.version;}
      const artifacts=document.getElementById('artifacts');artifacts.replaceChildren();
      for(const item of d.artifacts){const li=document.createElement('li'),a=document.createElement('a');a.href=item.url;a.target='_blank';a.rel='noopener';a.textContent=item.name;li.append(a);artifacts.append(li);}
      const diff=d.artifacts.filter(a=>a.name.includes('diff')&&a.name.endsWith('.patch')).at(-1);
      if(diff&&diff.url!==lastDiff){const r=await fetch(diff.url);if(r.ok){text('diff-preview',await r.text()||'没有代码差异');lastDiff=diff.url;}}
    }catch(error){text('poll-status','连接暂时中断，正在重试；已保存的历史不会丢失。 '+error.message);}
    finally{busy=false;setTimeout(update,2000);}
  }
  for(const form of document.querySelectorAll('.control-buttons form'))form.addEventListener('submit',()=>{for(const b of form.querySelectorAll('button'))b.disabled=true;});
  update();
})();
