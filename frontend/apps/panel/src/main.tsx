import React from 'react';
import { createRoot } from 'react-dom/client';
import '@shapi/ui/style.css';
import UI from './paginas/_UI';

createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <UI />
  </React.StrictMode>
);
