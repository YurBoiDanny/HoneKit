import { describe, expect, it } from 'vitest';
import { describePtzAction, formatPtzAction, parseStringList, readPtzAction } from './ptz';

describe('PTZ actions', () => {
  it('formats the camera, verb and number in the submitted order', () => {
    expect(formatPtzAction(3, 'preset', 10)).toBe('cam3:preset:10');
    expect(JSON.stringify([formatPtzAction(3, 'preset', 10), formatPtzAction(1, 'trace', 2)])).toBe(
      '["cam3:preset:10","cam1:trace:2"]',
    );
  });

  it('round-trips an action and explains it', () => {
    expect(readPtzAction('cam3:preset:10')).toEqual({
      camera: 3,
      verb: 'preset',
      number: 10,
      raw: 'cam3:preset:10',
    });
    expect(describePtzAction('cam3:preset:10')).toBe('Camera 3, preset 10');
  });

  it('leaves unknown text alone', () => {
    expect(readPtzAction('preset 10')).toBeNull();
    expect(describePtzAction('preset 10')).toBe('preset 10');
  });
});

describe('parseStringList', () => {
  it('reads the JSON arrays the server uses for an answer reveal', () => {
    expect(parseStringList('["s-1","s-0"]')).toEqual(['s-1', 's-0']);
    expect(parseStringList('["cam1:preset:1"]')).toEqual(['cam1:preset:1']);
  });

  it('returns null for plain text and other JSON shapes', () => {
    expect(parseStringList('1. Power on\n2. Frame the shot')).toBeNull();
    expect(parseStringList('{"a":1}')).toBeNull();
    expect(parseStringList('[1,2]')).toBeNull();
  });
});
