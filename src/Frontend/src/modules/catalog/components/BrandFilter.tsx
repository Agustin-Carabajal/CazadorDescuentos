import React from 'react';

interface BrandFilterProps {
  selectedBrand: string;
  onSelectBrand: (brand: string) => void;
  totalCount: number;
}

export const BrandFilter: React.FC<BrandFilterProps> = ({ selectedBrand, onSelectBrand, totalCount }) => {
  return (
    <div className="flex flex-wrap gap-2 w-full md:w-auto">
      <button
        onClick={() => onSelectBrand('all')}
        type="button"
        className={`px-4 py-2 rounded-lg text-sm font-semibold transition border ${
          selectedBrand === 'all'
            ? 'bg-slate-900 text-white border-slate-900 shadow-sm'
            : 'bg-slate-50 text-slate-700 border-slate-200 hover:bg-slate-100'
        }`}
      >
        Todas ({totalCount})
      </button>

      <button
        onClick={() => onSelectBrand("McDonald's")}
        type="button"
        className={`px-4 py-2 rounded-lg text-sm font-semibold transition border ${
          selectedBrand === "McDonald's"
            ? 'bg-red-600 text-yellow-300 border-red-600 shadow-sm'
            : 'bg-red-50 text-red-700 border-red-200 hover:bg-red-100'
        }`}
      >
        🍟 McDonald's
      </button>

      <button
        onClick={() => onSelectBrand('Burger King')}
        type="button"
        className={`px-4 py-2 rounded-lg text-sm font-semibold transition border ${
          selectedBrand === 'Burger King'
            ? 'bg-amber-700 text-amber-100 border-amber-700 shadow-sm'
            : 'bg-amber-50 text-amber-800 border-amber-200 hover:bg-amber-100'
        }`}
      >
        👑 Burger King
      </button>

      <button
        onClick={() => onSelectBrand('Mostaza')}
        type="button"
        className={`px-4 py-2 rounded-lg text-sm font-semibold transition border ${
          selectedBrand === 'Mostaza'
            ? 'bg-rose-700 text-white border-rose-700 shadow-sm'
            : 'bg-rose-50 text-rose-800 border-rose-200 hover:bg-rose-100'
        }`}
      >
        🍔 Mostaza
      </button>
    </div>
  );
};
