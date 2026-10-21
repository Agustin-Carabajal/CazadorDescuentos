import React, { useState } from 'react';
import { catalogService } from '../../modules/catalog/services/catalogService';

interface NavbarProps {
  onRefresh: () => void;
  onNotice: (message: string) => void;
}

export const Navbar: React.FC<NavbarProps> = ({ onRefresh, onNotice }) => {
  const [scraping, setScraping] = useState<boolean>(false);

  const handleScrape = async () => {
    setScraping(true);
    try {
      const result = await catalogService.triggerScrape();
      onNotice(result.message);
      onRefresh();
    } catch {
      onNotice('Error al ejecutar el scraper automático.');
    } finally {
      setScraping(false);
    }
  };

  return (
    <header className="bg-white border-b border-slate-200 sticky top-0 z-40 shadow-xs">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-3 flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <span className="text-3xl">🍔</span>
          <div>
            <h1 className="text-xl font-bold text-slate-800 tracking-tight">Cazador de Descuentos</h1>
            <p className="text-xs text-slate-500 font-medium">
              Comparador de ofertas en tiempo real: McDonald’s • Burger King • Mostaza
            </p>
          </div>
        </div>
        <div className="flex items-center space-x-3">
          <button
            onClick={handleScrape}
            disabled={scraping}
            type="button"
            className="text-xs bg-slate-100 hover:bg-slate-200 text-slate-700 px-3 py-2 rounded-lg font-medium transition flex items-center gap-1.5 border border-slate-300 disabled:opacity-50"
          >
            <i className={`fa-solid fa-arrows-rotate ${scraping ? 'fa-spin' : ''}`}></i>
            <span>{scraping ? 'Extrayendo...' : 'Actualizar Ofertas'}</span>
          </button>
        </div>
      </div>
    </header>
  );
};
