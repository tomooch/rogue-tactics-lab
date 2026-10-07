import {simulate,CONDITIONS} from '../core.mjs';
for(const key of Object.keys(CONDITIONS))for(const tactic of ['focus','split'])console.log(key,tactic,simulate(key,tactic).result);
