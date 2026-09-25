import { setupServer } from 'msw/node';

export const server = setupServer();

import { beforeAll, afterEach, afterAll } from 'vitest';

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
