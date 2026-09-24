import { Boton, Campo, Etiqueta, Tarjeta, Selector, Aviso } from '@shapi/ui';

export default function UI() {
  return (
    <div className="p-12 space-y-12 bg-[#f1f1f1] min-h-screen text-[#1a1a1a]">
      <header>
        <h1 className="text-3xl font-display font-bold mb-2">Lámina de Estilo Oficial (DC-01)</h1>
        <p className="text-gray-500 font-sans">Catálogo interactivo de los componentes base y elementos visuales del sistema de diseño.</p>
      </header>
      
      <section className="space-y-4">
        <h2 className="text-xl font-display font-semibold border-b border-gray-200 pb-2">Botones y Acciones</h2>
        <div className="flex gap-4 items-center">
          <Boton principal={true}>Principal</Boton>
          <Boton principal={false} className="!h-8 !px-3 !bg-[#e3e3e3] hover:!bg-[#d4d4d4] !border-none !text-sm !font-semibold !rounded-md">Export</Boton>
          <Boton principal={false} className="!h-8 !px-3 !bg-[#e3e3e3] hover:!bg-[#d4d4d4] !border-none !text-sm !font-semibold !rounded-md">Import</Boton>
          <Boton deshabilitado={true}>Deshabilitado</Boton>
        </div>
      </section>

      <section className="space-y-4">
        <h2 className="text-xl font-display font-semibold border-b border-gray-200 pb-2">Etiquetas de Estado (Tags)</h2>
        <div className="flex gap-4 items-center">
          <Etiqueta estado="correcto">Active</Etiqueta>
          <Etiqueta estado="info">Draft</Etiqueta>
          <Etiqueta estado="neutro">Archived</Etiqueta>
          <Etiqueta estado="alerta">Error / Inactive</Etiqueta>
        </div>
      </section>

      <section className="space-y-4 max-w-sm">
        <h2 className="text-xl font-display font-semibold border-b border-gray-200 pb-2">Campos y Formularios</h2>
        <Campo placeholder="Texto normal" />
        <Campo placeholder="Búsqueda..." />
        <Campo placeholder="Con error" error="Este campo es obligatorio" />
        <Selector options={[{label: 'Opción 1', value: '1'}]} value="1" onChange={()=>{}} />
      </section>

      <section className="space-y-4 max-w-2xl">
        <h2 className="text-xl font-display font-semibold border-b border-gray-200 pb-2">Avisos y Tarjetas de Estadísticas</h2>
        <Tarjeta className="p-0 flex overflow-hidden bg-white rounded-xl shadow-sm border border-gray-200">
          <div className="flex-1 p-4 border-r border-gray-200">
            <div className="text-sm font-medium text-gray-600 mb-1">Products by sell-through rate</div>
            <div className="flex items-baseline gap-2">
              <span className="text-xl font-semibold">0%</span>
              <span className="text-gray-400">—</span>
            </div>
          </div>
          <div className="flex-1 p-4 border-r border-gray-200">
            <div className="text-sm font-medium text-gray-600 mb-1">Products by days of inventory remaining</div>
            <div className="text-sm text-gray-500 mt-1">There was no data found for this date range</div>
          </div>
        </Tarjeta>
        <Aviso estado="info">Recuerda completar el perfil</Aviso>
      </section>

      <section className="space-y-4 max-w-5xl">
        <h2 className="text-xl font-display font-semibold border-b border-gray-200 pb-2">Tabla de Datos (Estilo Mockup)</h2>
        
        <Tarjeta className="p-0 overflow-hidden bg-white rounded-xl shadow-sm border border-gray-200">
          {/* Tabs */}
          <div className="flex items-center gap-1 p-2 border-b border-gray-200 text-sm font-medium">
            <button className="px-3 py-1.5 bg-[#f1f1f1] rounded-md text-gray-800">All</button>
            <button className="px-3 py-1.5 text-gray-500 hover:bg-gray-50 rounded-md">Active</button>
            <button className="px-3 py-1.5 text-gray-500 hover:bg-gray-50 rounded-md">Draft</button>
            <button className="px-3 py-1.5 text-gray-500 hover:bg-gray-50 rounded-md">Archived</button>
            <button className="px-3 py-1.5 text-gray-500 hover:bg-gray-50 rounded-md">+</button>
          </div>

          {/* Table */}
          <div className="w-full overflow-auto">
            <table className="w-full text-left border-collapse text-sm">
              <thead>
                <tr className="border-b border-gray-200">
                  <th className="py-2.5 px-4 font-semibold text-gray-600 w-12 text-center">
                    <input type="checkbox" className="rounded border-gray-300" />
                  </th>
                  <th className="py-2.5 px-4 font-semibold text-gray-600">Product <span className="text-xs">↕</span></th>
                  <th className="py-2.5 px-4 font-semibold text-gray-600">Status</th>
                  <th className="py-2.5 px-4 font-semibold text-gray-600">Inventory</th>
                  <th className="py-2.5 px-4 font-semibold text-gray-600 text-right">Sales channels</th>
                </tr>
              </thead>
              <tbody>
                <tr className="border-b border-gray-200 hover:bg-gray-50">
                  <td className="py-3 px-4 text-center">
                    <input type="checkbox" className="rounded border-gray-300" />
                  </td>
                  <td className="py-3 px-4 flex items-center gap-3">
                    <div className="w-9 h-9 rounded bg-gray-100 border border-gray-200 flex items-center justify-center shrink-0 overflow-hidden">
                       <img src="https://placehold.co/100x100/F5EFE6/967E76?text=Soap" alt="" className="w-full h-full object-cover" />
                    </div>
                    <span className="font-medium text-gray-800">(Sample) Coconut Bar Soap</span>
                  </td>
                  <td className="py-3 px-4">
                    <Etiqueta estado="correcto">Active</Etiqueta>
                  </td>
                  <td className="py-3 px-4 text-[#d82c0d] font-medium">0 in stock</td>
                  <td className="py-3 px-4 text-right text-gray-600">1</td>
                </tr>
                <tr className="border-b border-gray-200 hover:bg-gray-50">
                  <td className="py-3 px-4 text-center">
                    <input type="checkbox" className="rounded border-gray-300" />
                  </td>
                  <td className="py-3 px-4 flex items-center gap-3">
                    <div className="w-9 h-9 rounded bg-gray-100 border border-gray-200 flex items-center justify-center shrink-0 overflow-hidden">
                       <img src="https://placehold.co/100x100/6b5b4f/ffffff?text=NB" alt="" className="w-full h-full object-cover" />
                    </div>
                    <span className="font-medium text-gray-800">Copy of Custom Notebook</span>
                  </td>
                  <td className="py-3 px-4">
                    <Etiqueta estado="info">Draft</Etiqueta>
                  </td>
                  <td className="py-3 px-4 text-[#d82c0d] font-medium">0 in stock for 24 variants</td>
                  <td className="py-3 px-4 text-right text-gray-600">3</td>
                </tr>
                <tr className="hover:bg-gray-50">
                  <td className="py-3 px-4 text-center">
                    <input type="checkbox" className="rounded border-gray-300" />
                  </td>
                  <td className="py-3 px-4 flex items-center gap-3">
                    <div className="w-9 h-9 rounded bg-gray-100 border border-gray-200 flex items-center justify-center shrink-0 overflow-hidden">
                       <img src="https://placehold.co/100x100/D0B8A8/ffffff?text=Hat" alt="" className="w-full h-full object-cover" />
                    </div>
                    <span className="font-medium text-gray-800">Example Hat</span>
                  </td>
                  <td className="py-3 px-4">
                    <Etiqueta estado="neutro">Archived</Etiqueta>
                  </td>
                  <td className="py-3 px-4 text-gray-500">Inventory not tracked</td>
                  <td className="py-3 px-4 text-right text-gray-600">2</td>
                </tr>
              </tbody>
            </table>
          </div>
        </Tarjeta>
      </section>
    </div>
  );
}
