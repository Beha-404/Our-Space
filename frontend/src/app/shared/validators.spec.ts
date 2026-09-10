import { isStrongPassword, isValidEmail } from './validators';

describe('isValidEmail', () => {
  it('accepts a normal address', () => {
    expect(isValidEmail('kerim@example.com')).toBe(true);
  });

  it('accepts a gmail + alias address', () => {
    expect(isValidEmail('kerim.beha12+test1@gmail.com')).toBe(true);
  });

  it('trims surrounding whitespace before validating', () => {
    expect(isValidEmail('  kerim@example.com  ')).toBe(true);
  });

  it('rejects a value with no @', () => {
    expect(isValidEmail('kerim.example.com')).toBe(false);
  });

  it('rejects a value with no domain suffix', () => {
    expect(isValidEmail('kerim@example')).toBe(false);
  });

  it('rejects a value containing spaces', () => {
    expect(isValidEmail('kerim @example.com')).toBe(false);
  });

  it('rejects an empty string', () => {
    expect(isValidEmail('')).toBe(false);
  });
});

describe('isStrongPassword', () => {
  it('accepts a password with upper, lower, digit and 9+ chars', () => {
    expect(isStrongPassword('Password123')).toBe(true);
  });

  it('rejects a password shorter than 9 characters', () => {
    expect(isStrongPassword('Pass1')).toBe(false);
  });

  it('rejects a password with no uppercase letter', () => {
    expect(isStrongPassword('password123')).toBe(false);
  });

  it('rejects a password with no lowercase letter', () => {
    expect(isStrongPassword('PASSWORD123')).toBe(false);
  });

  it('rejects a password with no digit', () => {
    expect(isStrongPassword('PasswordOnly')).toBe(false);
  });
});
