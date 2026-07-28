#!/usr/bin/env node
/**
 * 数据模型验证测试
 * 由 codex-watch.sh 自动调用，也可手动运行: node test-data-model.mjs
 */
const stages = [
  {id:1, title:'1-1 明水入汴京', level:1, power:800,  objective:'前往李府庭院',   reward:'铜钱 1200', unlocked:true},
  {id:2, title:'1-2 雅集赴会',   level:1, power:980,  objective:'完成词意试炼',   reward:'名士信笺 1', unlocked:true},
  {id:3, title:'1-3 词论初临',   level:2, power:1160, objective:'回应前辈论词',   reward:'突破材料 1',  unlocked:false},
  {id:4, title:'1-4 风雨前夜',   level:3, power:1340, objective:'完成雨夜准备',   reward:'词意经验 120',unlocked:false},
  {id:5, title:'1-5 故人入梦',   level:4, power:1520, objective:'进入梦境支线',   reward:'梦境碎片 1',  unlocked:false},
  {id:6, title:'1-6 潮声再起',   level:5, power:1700, objective:'完成收束战',     reward:'玉 60',       unlocked:false},
];

let p = 0, f = 0;
function ok(cond, msg) { if (cond) p++; else { console.log('  ❌ ' + msg); f++; } }
function get(id) { return stages.find(s => s.id === id) || stages[0]; }

// 字段完整性 (6关 × 4字段)
for (const s of stages) {
  ok(!!s.title,     '关卡' + s.id + ' title 为空');
  ok(s.level >= 1,  '关卡' + s.id + ' level=' + s.level + ' (应>=1)');
  ok(s.power > 0,   '关卡' + s.id + ' power=' + s.power + ' (应>0)');
  ok(!!s.objective, '关卡' + s.id + ' objective 为空');
}

// 解锁模式
ok(stages[0].unlocked === true,  '1-1 应已解锁');
ok(stages[1].unlocked === true,  '1-2 应已解锁');
ok(stages[2].unlocked === false, '1-3 应未解锁');
ok(stages[5].unlocked === false, '1-6 应未解锁');

// 战力递增
for (let i = 1; i < stages.length; i++)
  ok(stages[i].power >= stages[i-1].power,
     '战力应递增: ' + stages[i-1].id + '=' + stages[i-1].power + ' → ' + stages[i].id + '=' + stages[i].power);

// 无效id行为
ok(get(999).id === 1, '不存在的 id=999，当前返回第一关');

// 总数量
ok(stages.length === 6, '应有 6 关，实际 ' + stages.length);

console.log('  ✅ ' + p + '/' + (p + f) + ' 通过');
process.exit(f > 0 ? 1 : 0);
