import assert from 'node:assert/strict';
import { readFileSync, existsSync } from 'node:fs';
import { join, resolve } from 'node:path';

const root = resolve(import.meta.dirname, '../..');
const source = join(root, 'CourseSources/English/english-life-office-it');
const published = join(root, 'src/LinguaFlow.Web/wwwroot/Courses/English/english-life-office-it');
const manifest = JSON.parse(readFileSync(join(published, 'manifest.json'), 'utf8'));
assert.equal(manifest.units.length, 36);
const totals = { units: 0, sections: 0, exercises: 0, objectives: 0, grammar: 0, vocabulary: 0, reading: 0, writing: 0, speaking: 0, listening: 0, homework: 0 };
const ids = new Set();
for (const reference of manifest.units) {
  const number = String(reference.order).padStart(2, '0');
  const lesson = readFileSync(join(source, `LESSONS/Unit_${number}.md`), 'utf8');
  const answers = join(source, `ANSWER_KEYS/Unit_${number}_Answers.md`);
  assert(existsSync(answers), `Missing source answer key ${number}`);
  const heading = lesson.match(/^# UNIT \d{2} [—-] (.+)$/m)?.[1]?.trim();
  assert.equal(reference.title, heading, `Source title differs for Unit ${number}`);
  assert.equal(reference.unitId, `unit-${number}`);
  const unit = JSON.parse(readFileSync(join(published, reference.path), 'utf8'));
  assert.equal(unit.unitId, reference.unitId);
  assert.equal(unit.title, heading);
  assert(unit.objectives?.length > 0, `Missing objectives in ${number}`);
  assert(unit.sections?.length > 0, `Missing sections in ${number}`);
  totals.units++; totals.objectives += unit.objectives.length; totals.sections += unit.sections.length;
  for (const section of unit.sections) {
    assert(!ids.has(`${number}:${section.sectionId}`), `Duplicate section in ${number}`);
    ids.add(`${number}:${section.sectionId}`);
    assert(section.source?.file?.startsWith('LESSONS/'), `Invalid provenance in ${number}`);
    for (const block of section.blocks) {
      assert(!ids.has(block.blockId), `Duplicate block ${block.blockId}`); ids.add(block.blockId);
      if (block.exerciseId) { assert(!ids.has(block.exerciseId), `Duplicate exercise ${block.exerciseId}`); ids.add(block.exerciseId); totals.exercises++; }
      const family = block.type?.toLowerCase(); if (family in totals) totals[family]++;
    }
  }
  if ([1, 12, 18, 30, 36].includes(reference.order)) {
    console.log(`Representative ${number}: ${heading}; ${unit.sections.length} sections; ${unit.objectives.length} objectives; ${unit.kind || 'lesson'}`);
  }
}
console.log(JSON.stringify(totals, null, 2));
