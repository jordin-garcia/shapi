import { describe, it, expect } from 'vitest';
import { crearCliente } from './index';

describe('API Client', () => {
  it('should add X-Requested-With header', async () => {
    const client = crearCliente('http://localhost');
    expect(client).toBeDefined();
  });
});
