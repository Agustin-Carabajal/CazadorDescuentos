import React from 'react';

interface BrandFilterProps {
  selectedBrand: string;
  onSelectBrand: (brand: string) => void;
  totalCount: number;
  mcCount?: number;
  bkCount?: number;
  mostazaCount?: number;
}

export const BrandFilter: React.FC<BrandFilterProps> = ({
  selectedBrand,
  onSelectBrand,
  totalCount,
  mcCount,
  bkCount,
  mostazaCount
}) => {
  return (
    <div className="flex flex-wrap items-center gap-2 w-full md:w-auto">
      <button
        onClick={() => onSelectBrand('all')}
        type="button"
        className={`px-4 py-2.5 rounded-xl text-xs sm:text-sm font-bold transition-all flex items-center gap-2 ${
          selectedBrand === 'all'
            ? 'bg-slate-900 text-white shadow-sm'
            : 'bg-slate-50 text-slate-700 border border-slate-200 hover:bg-slate-100'
        }`}
      >
        <span>Todas las Cadenas</span>
        <span className="bg-white/20 text-xs px-2 py-0.5 rounded-full font-mono">{totalCount}</span>
      </button>

      <button
        onClick={() => onSelectBrand("McDonald's")}
        type="button"
        className={`px-4 py-2.5 rounded-xl text-xs sm:text-sm font-bold transition-all flex items-center gap-2 ${
          selectedBrand === "McDonald's"
            ? 'bg-red-600 text-yellow-300 shadow-sm shadow-red-600/30'
            : 'bg-slate-50 text-slate-700 border border-slate-200 hover:bg-red-50 hover:text-red-700'
        }`}
      >
        <span>🍟 McDonald's</span>
        {mcCount !== undefined && (
          <span className="bg-red-100 text-red-800 text-xs px-2 py-0.5 rounded-full font-mono">{mcCount}</span>
        )}
      </button>

      <button
        onClick={() => onSelectBrand('Burger King')}
        type="button"
        className={`px-4 py-2.5 rounded-xl text-xs sm:text-sm font-bold transition-all flex items-center gap-2 ${
          selectedBrand === 'Burger King'
            ? 'bg-amber-700 text-white shadow-sm shadow-amber-700/30'
            : 'bg-slate-50 text-slate-700 border border-slate-200 hover:bg-amber-50 hover:text-amber-800'
        }`}
      >
        <span>👑 Burger King</span>
        {bkCount !== undefined && (
          <span className="bg-amber-100 text-amber-800 text-xs px-2 py-0.5 rounded-full font-mono">{bkCount}</span>
        )}
      </button>

      <button
        onClick={() => onSelectBrand('Mostaza')}
        type="button"
        className={`px-4 py-2.5 rounded-xl text-xs sm:text-sm font-bold transition-all flex items-center gap-2 ${
          selectedBrand === 'Mostaza'
            ? 'bg-rose-700 text-white shadow-sm shadow-rose-700/30'
            : 'bg-slate-50 text-slate-700 border border-slate-200 hover:bg-rose-50 hover:text-rose-800'
        }`}
      >
        <span>🍔 Mostaza</span>
        {mostazaCount !== undefined && (
          <span className="bg-rose-100 text-rose-800 text-xs px-2 py-0.5 rounded-full font-mono">{mostazaCount}</span>
        )}
      </button>
    </div>
  );
};
