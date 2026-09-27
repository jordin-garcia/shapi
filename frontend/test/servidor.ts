import { setupServer } from 'msw/node';

export const server = setupServer();

import { beforeAll, afterEach, afterAll } from 'vitest';

// Una petición que ninguna prueba simula es un error: así no pasa desapercibida una llamada inesperada a la API.
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
