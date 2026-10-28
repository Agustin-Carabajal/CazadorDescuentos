import React, { useState, useEffect } from 'react';
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
  const [toastTitle, setToastTitle] = useState<string>('Notificación');
  const [searchInput, setSearchInput] = useState<string>('');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [selectedBenefit, setSelectedBenefit] = useState<string>('all');
  const [selectedChannel, setSelectedChannel] = useState<string>('all');
  const [viewMode, setViewMode] = useState<'cards' | 'table'>('cards');

  // Auto-poll every 25 seconds to catch 5-minute background scraper updates automatically
  useEffect(() => {
    const pollInterval = setInterval(() => {
      refetch();
    }, 25000);
    return () => clearInterval(pollInterval);
  }, [refetch]);

  const showToast = (message: string, title = 'Éxito') => {
    setToastTitle(title);
    setToastMessage(message);
    setTimeout(() => setToastMessage(null), 3500);
  };

  const handleOpenReport = (offer: Offer) => {
    setSelectedOfferForReport(offer);
    setIsReportModalOpen(true);
  };

  // Filter & Search computation
  const filteredOffers = offers.filter(o => {
    const matchesBrand = !filter.brand || filter.brand === 'all' || o.brand === filter.brand;
    const matchesCategory = selectedCategory === 'all' || o.category === selectedCategory;
    const matchesBenefit = selectedBenefit === 'all' || o.benefitType === selectedBenefit;
    const matchesChannel =
      selectedChannel === 'all' || o.channel === selectedChannel || o.channel === 'App / Web';

    const q = searchInput.trim().toLowerCase();
    const matchesSearch =
      !q ||
      o.title.toLowerCase().includes(q) ||
      o.brand.toLowerCase().includes(q) ||
      o.category.toLowerCase().includes(q) ||
      (o.promoCode && o.promoCode.toLowerCase().includes(q)) ||
      (o.benefitDescription && o.benefitDescription.toLowerCase().includes(q));

    return matchesBrand && matchesCategory && matchesBenefit && matchesChannel && matchesSearch;
  });

  // Brand counts
  const mcCount = offers.filter(o => o.brand.includes("McDonald")).length;
  const bkCount = offers.filter(o => o.brand.includes("Burger")).length;
  const mostazaCount = offers.filter(o => o.brand.includes("Mostaza")).length;

  return (
    <div className="min-h-screen flex flex-col bg-slate-100 text-slate-800 font-sans">
      <Navbar onRefresh={refetch} onNotice={msg => showToast(msg, 'Extracción en vivo')} />

      <main className="flex-grow max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-6">
        {/* Hero Section */}
        <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-slate-950 via-slate-900 to-red-950 text-white shadow-2xl mb-8 p-6 sm:p-10 border border-slate-800">
          <div className="relative z-10 max-w-3xl">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-red-600/30 border border-red-500/40 text-red-300 text-xs font-bold mb-4 backdrop-blur-sm">
              <span className="inline-block w-2 h-2 rounded-full bg-red-500 animate-pulse"></span>
              COMPARADOR AUTOMATIZADO CON IA EN ARGENTINA
            </div>

            <h2 className="text-3xl sm:text-5xl font-extrabold tracking-tight leading-tight mb-4">
              Caza las mejores ofertas en <br className="hidden sm:inline" />
              <span className="text-transparent bg-clip-text bg-gradient-to-r from-yellow-400 via-amber-300 to-red-400">
                McDonald's, BK y Mostaza
              </span>
            </h2>

            <p className="text-slate-300 text-sm sm:text-base leading-relaxed mb-6 max-w-2xl">
              Comparamos en tiempo real combos, 2x1, cupones y beneficios. Precios normalizados en pesos argentinos con enlaces oficiales verificados y copiado rápido de cupón.
            </p>

            {/* Stats Counter Bar */}
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 pt-4 border-t border-slate-800/80">
              <div className="bg-slate-900/60 backdrop-blur-sm p-3 rounded-2xl border border-slate-800">
                <p className="text-[11px] text-slate-400 font-medium">Ofertas Activas</p>
                <p className="text-xl sm:text-2xl font-black text-amber-400">{offers.length}</p>
              </div>
              <div className="bg-slate-900/60 backdrop-blur-sm p-3 rounded-2xl border border-slate-800">
                <p className="text-[11px] text-slate-400 font-medium">Ahorro Máximo</p>
                <p className="text-xl sm:text-2xl font-black text-emerald-400">Hasta 50%</p>
              </div>
              <div className="bg-slate-900/60 backdrop-blur-sm p-3 rounded-2xl border border-slate-800">
                <p className="text-[11px] text-slate-400 font-medium">Sincronización</p>
                <p className="text-xl sm:text-2xl font-black text-sky-400">Cada 5 min</p>
              </div>
              <div className="bg-slate-900/60 backdrop-blur-sm p-3 rounded-2xl border border-slate-800">
                <p className="text-[11px] text-slate-400 font-medium">Marcas Líderes</p>
                <p className="text-xl sm:text-2xl font-black text-rose-400">3 Cadenas</p>
              </div>
            </div>
          </div>
        </div>

        {/* Interactive Filter Control Panel */}
        <div className="bg-white rounded-2xl shadow-sm border border-slate-200/80 p-5 mb-8">
          <div className="flex flex-col gap-4">
            {/* Row 1: Brand Tabs & View Mode */}
            <div className="flex flex-col lg:flex-row items-stretch lg:items-center justify-between gap-4">
              <BrandFilter
                selectedBrand={filter.brand || 'all'}
                onSelectBrand={brand => updateFilter({ brand })}
                totalCount={offers.length}
                mcCount={mcCount}
                bkCount={bkCount}
                mostazaCount={mostazaCount}
              />

              {/* View Switcher */}
              <div className="flex items-center gap-1 bg-slate-100 p-1 rounded-xl self-start lg:self-auto border border-slate-200">
                <button
                  type="button"
                  onClick={() => setViewMode('cards')}
                  className={`px-3.5 py-1.5 rounded-lg text-xs font-bold transition flex items-center gap-1.5 ${
                    viewMode === 'cards'
                      ? 'bg-white text-slate-900 shadow-xs'
                      : 'text-slate-500 hover:text-slate-900'
                  }`}
                >
                  <i className="fa-solid fa-table-cells-large"></i>
                  <span>Tarjetas</span>
                </button>
                <button
                  type="button"
                  onClick={() => setViewMode('table')}
                  className={`px-3.5 py-1.5 rounded-lg text-xs font-bold transition flex items-center gap-1.5 ${
                    viewMode === 'table'
                      ? 'bg-white text-slate-900 shadow-xs'
                      : 'text-slate-500 hover:text-slate-900'
                  }`}
                >
                  <i className="fa-solid fa-table-list"></i>
                  <span>Tabla Comparativa</span>
                </button>
              </div>
            </div>

            {/* Row 2: Category Chips */}
            <div className="flex items-center gap-1.5 overflow-x-auto pb-1 scrollbar-none text-xs">
              {[
                { id: 'all', label: 'Todas las categorías' },
                { id: 'Combo', label: '🍔 Combos' },
                { id: 'Hamburguesa', label: '🥩 Hamburguesas' },
                { id: 'Postre', label: '🍦 Postres y Helados' },
                { id: 'Pollo', label: '🍗 Pollo Crispy' },
                { id: 'Papas y Snacks', label: '🍟 Papas y Snacks' },
                { id: 'Desayuno y Merienda', label: '☕ Desayuno y Café' }
              ].map(cat => (
                <button
                  key={cat.id}
                  onClick={() => setSelectedCategory(cat.id)}
                  type="button"
                  className={`shrink-0 px-3 py-1.5 rounded-lg font-semibold transition ${
                    selectedCategory === cat.id
                      ? 'bg-red-600 text-white'
                      : 'bg-slate-100 hover:bg-slate-200 text-slate-700'
                  }`}
                >
                  {cat.label}
                </button>
              ))}
            </div>

            {/* Row 3: Instant Search & Dropdowns */}
            <div className="flex flex-col md:flex-row items-center justify-between gap-3 pt-3 border-t border-slate-100">
              <div className="relative w-full md:w-80">
                <i className="fa-solid fa-magnifying-glass absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400 text-xs"></i>
                <input
                  type="text"
                  value={searchInput}
                  onChange={e => setSearchInput(e.target.value)}
                  placeholder="Buscar por producto, cupón o beneficio..."
                  className="w-full pl-9 pr-3 py-2 text-xs sm:text-sm bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-red-500 focus:bg-white transition"
                />
              </div>

              <div className="flex flex-wrap sm:flex-nowrap items-center gap-2 w-full md:w-auto">
                <select
                  value={selectedBenefit}
                  onChange={e => setSelectedBenefit(e.target.value)}
                  className="text-xs sm:text-sm py-2 px-3 bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-red-500 font-medium"
                >
                  <option value="all">Todos los beneficios</option>
                  <option value="TwoForOne">🔥 2x1</option>
                  <option value="PercentageDiscount">🏷️ % de Descuento</option>
                  <option value="FreeGift">🎁 Regalos Gratis</option>
                  <option value="FixedAmount">💵 Precio Especial</option>
                </select>

                <select
                  value={selectedChannel}
                  onChange={e => setSelectedChannel(e.target.value)}
                  className="text-xs sm:text-sm py-2 px-3 bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-red-500 font-medium"
                >
                  <option value="all">Todos los canales</option>
                  <option value="App">📱 Solo App</option>
                  <option value="Web">🌐 Solo Web</option>
                </select>

                <select
                  value={filter.sortBy || 'price_asc'}
                  onChange={e => updateFilter({ sortBy: e.target.value })}
                  className="text-xs sm:text-sm py-2 px-3 bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-red-500 font-medium"
                >
                  <option value="price_asc">Menor precio</option>
                  <option value="price_desc">Mayor precio</option>
                  <option value="newest">Más recientes</option>
                </select>
              </div>
            </div>
          </div>
        </div>

        {/* Counter indicator */}
        <div className="flex items-center justify-between mb-4 px-1 text-xs text-slate-500 font-medium">
          <div>
            Mostrando <span className="font-bold text-slate-800">{filteredOffers.length}</span> de {offers.length} ofertas
          </div>
          <div className="flex items-center gap-1.5 text-emerald-600 font-semibold">
            <i className="fa-solid fa-bolt-lightning text-xs"></i>
            <span>Sincronización en vivo cada 5 min activa</span>
          </div>
        </div>

        {/* Comparative Cards / Table View */}
        <ComparisonTable
          offers={filteredOffers}
          loading={loading}
          viewMode={viewMode}
          onReportClick={handleOpenReport}
          onNotice={msg => showToast(msg, 'Cupón Copiado')}
          onTriggerScrape={refetch}
        />
      </main>

      {/* Report Modal */}
      <ReportModal
        offer={selectedOfferForReport}
        isOpen={isReportModalOpen}
        onClose={() => setIsReportModalOpen(false)}
        onSuccess={msg => showToast(msg, 'Reporte Enviado')}
      />

      {/* Floating Toast Alert */}
      {toastMessage && (
        <div className="fixed bottom-6 right-6 bg-slate-900/95 backdrop-blur-md text-white px-4 py-3.5 rounded-2xl shadow-2xl flex items-center gap-3 z-50 text-sm border border-slate-800 animate-in fade-in slide-in-from-bottom-2">
          <div className="w-8 h-8 rounded-full bg-emerald-500/20 text-emerald-400 flex items-center justify-center shrink-0">
            <i className="fa-solid fa-circle-check text-base"></i>
          </div>
          <div>
            <p className="font-bold text-xs text-white">{toastTitle}</p>
            <p className="text-xs text-slate-300">{toastMessage}</p>
          </div>
        </div>
      )}

      {/* Footer */}
      <footer className="bg-slate-950 text-slate-400 border-t border-slate-900 py-10 mt-16 text-xs">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex flex-col md:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <span className="text-2xl">🍔</span>
            <div>
              <p className="font-bold text-white text-sm">Cazador de Descuentos Argentina</p>
              <p className="text-slate-500">Centralizador y comparador automatizado de ofertas y cupones de comida rápida.</p>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
};
