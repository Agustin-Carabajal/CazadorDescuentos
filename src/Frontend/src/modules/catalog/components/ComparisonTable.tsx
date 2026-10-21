import React from 'react';
import { Offer } from '../services/catalogService';
import { ChannelBadge } from './ChannelBadge';
import { PromoCodeButton } from './PromoCodeButton';
import { ReportButton } from '../../reports/components/ReportButton';

interface ComparisonTableProps {
  offers: Offer[];
  loading: boolean;
  onReportClick: (offer: Offer) => void;
  onNotice: (message: string) => void;
  onTriggerScrape: () => void;
}

export const ComparisonTable: React.FC<ComparisonTableProps> = ({
  offers,
  loading,
  onReportClick,
  onNotice,
  onTriggerScrape
}) => {
  if (loading) {
    return (
      <div className="bg-white rounded-2xl border border-slate-200 p-12 text-center shadow-sm">
        <i className="fa-solid fa-spinner fa-spin text-3xl text-red-600 mb-3"></i>
        <p className="text-sm text-slate-500 font-medium">Cargando y comparando las ofertas disponibles...</p>
      </div>
    );
  }

  if (offers.length === 0) {
    return (
      <div className="bg-white rounded-2xl border border-slate-200 p-12 text-center shadow-sm">
        <span className="text-5xl mb-4 block">🔍</span>
        <h3 className="text-lg font-bold text-slate-800 mb-1">No se encontraron ofertas</h3>
        <p className="text-sm text-slate-500 max-w-md mx-auto mb-6">
          Prueba cambiando los filtros o ejecuta el scraper automático para extraer promociones recientes.
        </p>
        <button
          onClick={onTriggerScrape}
          type="button"
          className="inline-flex items-center gap-2 bg-red-600 hover:bg-red-700 text-white font-semibold text-sm px-5 py-2.5 rounded-xl shadow-md transition"
        >
          <i className="fa-solid fa-arrows-rotate"></i>
          <span>Ejecutar Scraper de Ofertas</span>
        </button>
      </div>
    );
  }

  return (
    <>
      {/* Desktop Comparative Table (Issue 6) */}
      <div className="hidden lg:block bg-white rounded-2xl shadow-sm border border-slate-200 overflow-hidden mb-8">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="bg-slate-100/75 border-b border-slate-200 text-xs font-bold text-slate-600 uppercase tracking-wider">
              <th className="py-4 px-6">Marca</th>
              <th className="py-4 px-6">Producto y Categoría</th>
              <th className="py-4 px-6 text-center">Tipo de Beneficio</th>
              <th className="py-4 px-6">Precio Normalizado</th>
              <th className="py-4 px-6">Canal / Cupón</th>
              <th className="py-4 px-6">Condiciones (IA)</th>
              <th className="py-4 px-6 text-center">Acciones</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 text-sm">
            {offers.map(offer => (
              <tr key={offer.id} className="hover:bg-slate-50/80 transition duration-150">
                {/* Brand */}
                <td className="py-4 px-6 whitespace-nowrap">
                  {offer.brand.includes("McDonald") ? (
                    <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-bold bg-red-600 text-yellow-300 shadow-xs">
                      🍟 McDonald's
                    </span>
                  ) : offer.brand.includes("Burger") ? (
                    <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-bold bg-amber-700 text-amber-100 shadow-xs">
                      👑 Burger King
                    </span>
                  ) : (
                    <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-bold bg-rose-700 text-white shadow-xs">
                      🍔 Mostaza
                    </span>
                  )}
                </td>

                {/* Product */}
                <td className="py-4 px-6">
                  <div className="font-bold text-slate-900">{offer.title}</div>
                  <div className="text-xs text-slate-500 flex items-center gap-2 mt-0.5">
                    <span className="bg-slate-100 text-slate-600 px-1.5 py-0.5 rounded text-[11px] font-medium">
                      {offer.category}
                    </span>
                    <span>Vence: {new Date(offer.validUntil).toLocaleDateString('es-AR')}</span>
                  </div>
                </td>

                {/* Benefit Type (Issue 4) */}
                <td className="py-4 px-6 text-center">
                  {offer.benefitType === 'TwoForOne' ? (
                    <span className="inline-block bg-purple-100 text-purple-700 font-extrabold px-3 py-1 rounded-full text-xs border border-purple-200">
                      🔥 2x1
                    </span>
                  ) : offer.benefitType === 'PercentageDiscount' ? (
                    <span className="inline-block bg-emerald-100 text-emerald-700 font-extrabold px-3 py-1 rounded-full text-xs border border-emerald-200">
                      {offer.benefitDescription || '% Descuento'}
                    </span>
                  ) : offer.benefitType === 'FreeGift' ? (
                    <span className="inline-block bg-amber-100 text-amber-700 font-extrabold px-3 py-1 rounded-full text-xs border border-amber-200">
                      🎁 Gratis
                    </span>
                  ) : (
                    <span className="inline-block bg-blue-100 text-blue-700 font-semibold px-3 py-1 rounded-full text-xs border border-blue-200">
                      🏷️ {offer.benefitDescription || 'Precio Especial'}
                    </span>
                  )}
                </td>

                {/* Price (Issue 2) */}
                <td className="py-4 px-6 whitespace-nowrap">
                  {offer.normalizedPrice > 0 ? (
                    <div>
                      <div className="text-lg font-black text-slate-900">
                        $ {offer.normalizedPrice.toLocaleString('es-AR')}
                      </div>
                      {offer.originalPrice && offer.originalPrice > offer.normalizedPrice && (
                        <div className="text-xs text-slate-400 line-through">
                          $ {offer.originalPrice.toLocaleString('es-AR')}
                        </div>
                      )}
                    </div>
                  ) : (
                    <span className="text-sm font-extrabold text-emerald-600 uppercase">Sin Cargo</span>
                  )}
                </td>

                {/* Channel & Promo Code (Issue 8) */}
                <td className="py-4 px-6 whitespace-nowrap">
                  <div className="mb-1.5">
                    <ChannelBadge channel={offer.channel} />
                  </div>
                  <PromoCodeButton promoCode={offer.promoCode} onCopyNotice={onNotice} />
                </td>

                {/* Conditions (Issue 3) */}
                <td className="py-4 px-6 text-xs text-slate-600 max-w-xs">
                  <p className="line-clamp-2" title={offer.termsAndConditions || ''}>
                    {offer.termsAndConditions || 'Consumo en local o take-away.'}
                  </p>
                </td>

                {/* Actions: Deep Link (Issue 7) + Report (Issue 9) */}
                <td className="py-4 px-6 text-center whitespace-nowrap">
                  <div className="flex items-center justify-center gap-2">
                    <a
                      href={offer.destinationUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-flex items-center gap-1 bg-red-600 hover:bg-red-700 text-white text-xs font-semibold px-3 py-1.5 rounded-lg shadow-xs transition"
                    >
                      <span>Ir a la Oferta</span>
                      <i className="fa-solid fa-arrow-up-right-from-square text-[10px]"></i>
                    </a>
                    <ReportButton onReportClick={() => onReportClick(offer)} />
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Mobile Card View Matrix */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:hidden gap-4 mb-8">
        {offers.map(offer => (
          <div key={offer.id} className="bg-white rounded-xl shadow-sm border border-slate-200 p-4 flex flex-col justify-between">
            <div>
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-bold">
                  {offer.brand}
                </span>
                <ChannelBadge channel={offer.channel} />
              </div>

              <h4 className="font-bold text-slate-900 text-base mb-1">{offer.title}</h4>
              <p className="text-xs text-slate-500 mb-3">{offer.description}</p>

              <div className="flex items-baseline gap-2 mb-3">
                {offer.normalizedPrice > 0 ? (
                  <span className="text-xl font-black text-slate-900">
                    $ {offer.normalizedPrice.toLocaleString('es-AR')}
                  </span>
                ) : (
                  <span className="text-base font-black text-emerald-600">GRATIS</span>
                )}
                {offer.originalPrice && offer.originalPrice > offer.normalizedPrice && (
                  <span className="text-xs text-slate-400 line-through">
                    $ {offer.originalPrice.toLocaleString('es-AR')}
                  </span>
                )}
              </div>

              <div className="mb-3">
                <PromoCodeButton promoCode={offer.promoCode} onCopyNotice={onNotice} />
              </div>

              {offer.termsAndConditions && (
                <p className="text-xs text-slate-500 italic bg-slate-50 p-2 rounded-lg border border-slate-100 mb-4">
                  {offer.termsAndConditions}
                </p>
              )}
            </div>

            <div className="flex items-center gap-2 pt-2 border-t border-slate-100">
              <a
                href={offer.destinationUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="flex-grow text-center bg-red-600 hover:bg-red-700 text-white text-xs font-bold py-2 rounded-lg transition"
              >
                Ir a la Oferta Oficial <i className="fa-solid fa-arrow-up-right-from-square ml-1"></i>
              </a>
              <ReportButton onReportClick={() => onReportClick(offer)} />
            </div>
          </div>
        ))}
      </div>
    </>
  );
};
