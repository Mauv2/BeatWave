from pathlib import Path
import json, math, csv, collections

folder=Path(__file__).resolve().parent
layers=json.loads((folder/'stage1_collider_layers.json').read_text())
summary={}
for phase,filename in [('main','stage1_geometry.json'),('cave','stage1_cave_geometry.json')]:
    d=json.loads((folder/filename).read_text())
    trace=d['trace']; rows=[]
    for n in d['notes']:
        n['phase']=phase
        n['before_event']=phase!='main' or n['time']<=trace[-1]['time']
        if phase=='main':
            n['all_layer_ground']=layers['allLayerHits'][n['index']-1]['name']
            n['all_layer_id']=layers['allLayerHits'][n['index']-1]['layer']
        if n['y'] is None:
            n['status']='NotCreated'
        elif phase=='main' and not n['before_event']:
            n['status']='AfterEvent'
        else:
            dx=d['upDx'] if n['lane']==0 else d['downDx']; dy=d['upDy'] if n['lane']==0 else d['downDy']
            best=(float('inf'),None)
            # Minimum over piecewise-linear 20ms physics samples, independent of input timing.
            for a,b in zip(trace,trace[1:]):
                ax=a['x']+dx; ay=a['y']+dy; vx=b['x']-a['x']; vy=b['y']-a['y']
                den=vx*vx+vy*vy
                u=max(0,min(1,((n['x']-ax)*vx+(n['y']-ay)*vy)/den)) if den else 0
                dist=math.hypot(ax+u*vx-n['x'],ay+u*vy-n['y'])
                if dist<best[0]: best=(dist,a['time']+u*(b['time']-a['time']))
            n['min_distance']=best[0]; n['best_time']=best[1]
            n['best_timing_ms']=(best[1]-n['time'])*1000
            target=min(trace,key=lambda f:abs(f['time']-n['time']))
            n['target_distance']=math.hypot(target['x']+dx-n['x'],target['y']+dy-n['y'])
            n['target_delta_y']=n['y']-target['y']-dy
            n['target_lag_x']=d['initialX']+n['time']*d['playerSpeed']-target['x']
            n['status']='Perfect' if best[0]<=.2 else 'Great' if best[0]<=.4 else 'Good' if best[0]<=.6 else 'Outside'
        rows.append(n)
    fields=list(dict.fromkeys(k for n in rows for k in n))
    with (folder/f'stage1_{phase}_notes.csv').open('w',encoding='utf-8-sig',newline='') as f:
        w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
    (folder/f'stage1_{phase}_note_analysis.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
    relevant=[n for n in rows if n['before_event']]
    summary[phase]={'total':len(rows),'generated':sum(n['y'] is not None for n in rows),'not_created':sum(n['y'] is None for n in rows),'considered_notes':len(relevant),'statuses':dict(collections.Counter(n['status'] for n in relevant)),'trace_end':trace[-1],'jump_hits':d['jumpHits'],'slope_examples':[n for n in relevant if n['y'] is not None and abs(n['angle'])>1], 'holds':[n for n in rows if n['kind']==1], 'max_horizontal_lag':max(d['initialX']+f['time']*d['playerSpeed']-f['x'] for f in trace)}
(folder/'stage1_summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:{kk:vv for kk,vv in v.items() if kk not in ('slope_examples','jump_hits','holds')} for k,v in summary.items()},ensure_ascii=False,indent=2))
print('Main slope examples:',[(n['index'],round(n['time'],3),round(n['angle'],2),round(n['min_distance'],4),n['status'],round(n['best_timing_ms'])) for n in summary['main']['slope_examples']])
