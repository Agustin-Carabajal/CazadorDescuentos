import React, { useState } from 'react';
import { useOffers } from './modules/catalog/hooks/useOffers';
import { Navbar } from './shared/components/Navbar';
import { BrandFilter } from './modules/catalog/components/BrandFilter';
import { ComparisonTable } from './modules/catalog/components/ComparisonTable';
import { ReportModal } from './modules/reports/components/ReportModal';
import { Offer } from './modules/catalog/services/catalogService';

export const App: React.FC = () => {
  const { offers, loading, filter, updateFilter, refetch } = useOffers();
  const [selectedOfferForReport, setSelectedOfferForReport] = useState<Offer | null>(null);
  const [isReportModalOpen, setIsReportModalOpen] = useState<boolean>(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);
  const [searchInput, setSearchInput] = useState<string>('');

  const showToast = (message: string) => {
    setToastMessage(message);
    setTimeout(() => setToastMessage(null), 3000);
  };

  const handleOpenReport = (offer: Offer) => {
    setSelectedOfferForReport(offer);
    setIsReportModalOpen(true);
  };

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateFilter({ searchQuery: searchInput });
  };

  return (
    <div className="min-h-screen flex flex-col bg-slate-50 text-slate-900 font-sans">
      <Navbar onRefresh={refetch} onNotice={showToast} />

      <main className="flex-grow max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-6">
        {/* Hero Section */}
        <div className="bg-gradient-to-r from-red-600 via-amber-600 to-yellow-500 rounded-2xl p-6 text-white shadow-lg mb-6">
          <h2 className="text-2xl sm:text-3xl font-black tracking-tight mb-2">Compara las Mejores Ofertas de Hoy</h2>
          <p className="text-white/90 text-sm max-w-2xl">
            Encuentra combos, 2x1 y descuentos en <strong>McDonald's, Burger King y Mostaza</strong> actualizados automáticamente con IA. Copia el cupón y ahorra al instante.
          </p>
        </div>

        {/* Filters Toolbar */}
        <div className="bg-white p-4 rounded-xl shadow-xs border border-slate-200 flex flex-col md:flex-row gap-4 items-center justify-between mb-8">
          <BrandFilter
            selectedBrand={filter.brand || 'all'}
            onSelectBrand={brand => updateFilter({ brand })}
            totalCount={offers.length}
          />

          <form onSubmit={handleSearchSubmit} className="flex flex-wrap sm:flex-nowrap gap-2 w-full md:w-auto items-center">
            <div className="relative w-full sm:w-64">
              <i className="fa-solid fa-magnifying-glass absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-xs"></i>
              <input
                type="text"
                value={searchInput}
                onChange={e => setSearchInput(e.target.value)}
                placeholder="Buscar hamburguesa, 2x1..."
                className="w-full pl-9 pr-3 py-2 text-sm bg-slate-50 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-red-500"
              />
            </div>

            <select
              value={filter.sortBy || 'newest'}
              onChange={e => updateFilter({ sortBy: e.target.value })}
              className="py-2 px-3 text-sm bg-slate-50 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-red-500"
            >
              <option value="newest">Más recientes</option>
              <option value="price_asc">Menor precio</option>
              <option value="price_desc">Mayor precio</option>
            </select>

            <button
              type="submit"
              className="px-4 py-2 text-sm font-medium bg-slate-800 text-white rounded-lg hover:bg-slate-700 transition"
            >
              Filtrar
            </button>
          </form>
        </div>

        {/* Matrix Comparison Table */}
        <ComparisonTable
          offers={offers}
          loading={loading}
          onReportClick={handleOpenReport}
          onNotice={showToast}
          onTriggerScrape={refetch}
        />
      </main>

      {/* Report Modal */}
      <ReportModal
        offer={selectedOfferForReport}
        isOpen={isReportModalOpen}
        onClose={() => setIsReportModalOpen(false)}
        onSuccess={msg => showToast(msg)}
      />

      {/* Toast Notification */}
      {toastMessage && (
        <div className="fixed bottom-6 right-6 bg-slate-900 text-white px-4 py-3 rounded-xl shadow-2xl flex items-center space-x-3 z-50 text-sm animate-fade-in">
          <i className="fa-solid fa-circle-check text-emerald-400 text-lg"></i>
          <span>{toastMessage}</span>
        </div>
      )}

      {/* Footer */}
      <footer className="bg-white border-t border-slate-200 py-6 mt-12 text-center text-xs text-slate-500">
        <div className="max-w-7xl mx-auto px-4 space-y-1">
          <p><strong>Cazador de Descuentos MVP</strong> — Centralizador automatizado de ofertas de comida rápida en Argentina.</p>
          <p>Precios normalizados en ARS. Los logos y nombres de marcas pertenecen a sus respectivos dueños.</p>
        </div>
      </footer>
    </div>
  );
};
