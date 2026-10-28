import React, { useState, useEffect } from 'react';
import { catalogService } from '../../modules/catalog/services/catalogService';

interface NavbarProps {
  onRefresh: () => void;
  onNotice: (message: string) => void;
}

export const Navbar: React.FC<NavbarProps> = ({ onRefresh, onNotice }) => {
  const [scraping, setScraping] = useState<boolean>(false);
  const [secondsRemaining, setSecondsRemaining] = useState<number>(300);

  useEffect(() => {
    const timer = setInterval(() => {
      setSecondsRemaining(prev => {
        if (prev <= 1) {
          onRefresh();
          return 300;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, [onRefresh]);

  const handleScrape = async () => {
    setScraping(true);
    try {
      const result = await catalogService.triggerScrape();
      onNotice(result.message);
      setSecondsRemaining(300);
      onRefresh();
    } catch {
      onNotice('Error al ejecutar el scraper automático.');
    } finally {
      setScraping(false);
    }
  };

  const minutes = Math.floor(secondsRemaining / 60);
  const seconds = secondsRemaining % 60;
  const formattedCountdown = `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;

  return (
    <>
      {/* Top Status Bar (5-minute countdown indicator) */}
      <div className="bg-slate-950 text-white border-b border-slate-800 px-4 py-2 text-xs">
        <div className="max-w-7xl mx-auto flex flex-wrap items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <span className="relative flex h-2 w-2">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
              <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
            </span>
            <span className="font-semibold text-slate-300">Sincronización en Tiempo Real:</span>
            <span className="text-slate-400 hidden sm:inline">McDonald's, BK y Mostaza actualizados cada 5 minutos.</span>
          </div>

          <div className="flex items-center gap-3 font-mono text-[11px]">
            <div className="flex items-center gap-1.5 text-slate-300 bg-slate-900 px-2.5 py-0.5 rounded-md border border-slate-800">
              <i className="fa-solid fa-clock-rotate-left text-amber-400"></i>
              <span>Próxima extracción en:</span>
              <span className="font-bold text-amber-400 text-xs">{formattedCountdown}</span>
            </div>
          </div>
        </div>
      </div>

      {/* Main Navigation Header */}
      <header className="bg-white/95 backdrop-blur-md border-b border-slate-200/80 sticky top-0 z-40 shadow-xs">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-3.5 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-red-600 to-amber-500 flex items-center justify-center text-white shadow-md text-xl">
              🍔
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="text-xl font-extrabold tracking-tight text-slate-900">
                  Cazador<span className="text-red-600">Descuentos</span>
                </span>
                <span className="text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 rounded-full bg-red-100 text-red-700">MVP</span>
              </div>
              <p className="text-[11px] text-slate-500 font-medium">McDonald’s • Burger King • Mostaza en Argentina</p>
            </div>
          </div>

          <div className="flex items-center gap-2.5">
            <button
              onClick={handleScrape}
              disabled={scraping}
              type="button"
              className="bg-red-600 hover:bg-red-700 active:bg-red-800 text-white text-xs font-bold px-3.5 py-2 rounded-xl shadow-sm transition-all flex items-center gap-2 disabled:opacity-50"
            >
              <i className={`fa-solid fa-arrows-rotate ${scraping ? 'fa-spin' : ''}`}></i>
              <span className="hidden sm:inline">{scraping ? 'Extrayendo...' : 'Actualizar Ahora'}</span>
            </button>
          </div>
        </div>
      </header>
    </>
  );
};
