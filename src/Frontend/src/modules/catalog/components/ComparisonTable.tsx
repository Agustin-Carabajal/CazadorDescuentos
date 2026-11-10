import React from 'react';
import { Offer } from '../services/catalogService';
import { ChannelBadge } from './ChannelBadge';
import { PromoCodeButton } from './PromoCodeButton';
import { ReportButton } from '../../reports/components/ReportButton';

interface ComparisonTableProps {
  offers: Offer[];
  loading: boolean;
  viewMode?: 'cards' | 'table';
  onReportClick: (offer: Offer) => void;
  onNotice: (message: string) => void;
  onTriggerScrape: () => void;
}

export const ComparisonTable: React.FC<ComparisonTableProps> = ({
  offers,
  loading,
  viewMode = 'cards',
  onReportClick,
  onNotice,
  onTriggerScrape
}) => {
  if (loading) {
    return (
      <div className="bg-white rounded-3xl border border-slate-200 p-16 text-center shadow-xs">
        <i className="fa-solid fa-spinner fa-spin text-4xl text-red-600 mb-4"></i>
        <h3 className="text-base font-bold text-slate-800 mb-1">Cargando y sincronizando ofertas...</h3>
        <p className="text-xs text-slate-500">Evaluando promociones y cupones vigentes en tiempo real.</p>
      </div>
    );
  }

  if (offers.length === 0) {
    return (
      <div className="bg-white rounded-3xl border border-slate-200 p-16 text-center shadow-xs">
        <div className="w-16 h-16 rounded-2xl bg-amber-100 text-amber-600 flex items-center justify-center text-3xl mx-auto mb-4">
          🔍
        </div>
        <h3 className="text-lg font-bold text-slate-800 mb-1">No se encontraron ofertas con estos filtros</h3>
        <p className="text-xs sm:text-sm text-slate-500 max-w-md mx-auto mb-6">
          Prueba cambiando los filtros o ejecuta la extracción automática para sincronizar promociones frescas.
        </p>
        <button
          onClick={onTriggerScrape}
          type="button"
          className="inline-flex items-center gap-2 bg-red-600 hover:bg-red-700 text-white font-bold text-xs px-5 py-2.5 rounded-xl shadow-md transition"
        >
          <i className="fa-solid fa-arrows-rotate"></i>
          <span>Ejecutar Extracción Ahora</span>
        </button>
      </div>
    );
  }

  return (
    <>
      {/* 1. CARDS VIEW (Modern visual fast food deal cards) */}
      {viewMode === 'cards' && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-12">
          {offers.map(offer => {
            const isMc = offer.brand.includes("McDonald");
            const isBk = offer.brand.includes("Burger");

            const discountAmount =
              offer.originalPrice && offer.originalPrice > offer.normalizedPrice
                ? offer.originalPrice - offer.normalizedPrice
                : 0;

            const discountPercent =
              offer.originalPrice && offer.originalPrice > offer.normalizedPrice && offer.originalPrice > 0
                ? Math.round((1.0 - offer.normalizedPrice / offer.originalPrice) * 100)
                : 0;

            return (
              <div
                key={offer.id}
                className="bg-white rounded-2xl shadow-xs border border-slate-200/80 p-5 flex flex-col justify-between hover:shadow-lg hover:-translate-y-1 transition-all duration-200"
              >
                <div>
                  {/* Top Header */}
                  <div className="flex items-center justify-between gap-2 mb-3">
                    {isMc ? (
                      <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-lg text-xs font-extrabold bg-red-600 text-yellow-300 shadow-xs">
                        <span>🍟</span>
                        <span>McDonald's</span>
                      </span>
                    ) : isBk ? (
                      <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-lg text-xs font-extrabold bg-amber-700 text-white shadow-xs">
                        <span>👑</span>
                        <span>Burger King</span>
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-lg text-xs font-extrabold bg-rose-700 text-white shadow-xs">
                        <span>🍔</span>
                        <span>Mostaza</span>
                      </span>
                    )}

                    <ChannelBadge channel={offer.channel} />
                  </div>

                  {/* Title & Category */}
                  <div className="mb-2">
                    <span className="inline-block text-[10px] font-bold tracking-wider uppercase px-2 py-0.5 rounded bg-slate-100 text-slate-600 mb-1">
                      {offer.category}
                    </span>
                    <h3 className="text-base sm:text-lg font-bold text-slate-900 leading-snug">
                      {offer.title}
                    </h3>
                  </div>

                  {/* Description */}
                  <p className="text-xs text-slate-500 line-clamp-2 mb-4 leading-relaxed">
                    {offer.description}
                  </p>

                  {/* Pricing & Benefit Tag */}
                  <div className="bg-slate-50 rounded-xl p-3.5 border border-slate-100 mb-4">
                    <div className="flex items-baseline justify-between mb-1.5">
                      <div className="flex items-baseline gap-2">
                        {offer.normalizedPrice > 0 ? (
                          <>
                            <span className="text-2xl font-black text-slate-950 font-mono tracking-tight">
                              $ {offer.normalizedPrice.toLocaleString('es-AR')}
                            </span>
                            {offer.originalPrice && offer.originalPrice > offer.normalizedPrice && (
                              <span className="text-xs text-slate-400 line-through font-mono">
                                $ {offer.originalPrice.toLocaleString('es-AR')}
                              </span>
                            )}
                          </>
                        ) : (
                          <span className="text-2xl font-black text-emerald-600 uppercase tracking-tight">
                            ¡Gratis!
                          </span>
                        )}
                      </div>

                      {/* Benefit Badge */}
                      <div>
                        {offer.benefitType === 'TwoForOne' ? (
                          <span className="inline-flex items-center gap-1 bg-purple-600 text-white font-extrabold px-2.5 py-0.5 rounded-md text-xs">
                            🔥 2x1
                          </span>
                        ) : offer.benefitType === 'PercentageDiscount' ? (
                          <span className="inline-flex items-center gap-1 bg-emerald-600 text-white font-extrabold px-2.5 py-0.5 rounded-md text-xs">
                            {offer.benefitDescription || `${discountPercent}% OFF`}
                          </span>
                        ) : offer.benefitType === 'FreeGift' ? (
                          <span className="inline-flex items-center gap-1 bg-amber-500 text-slate-950 font-black px-2.5 py-0.5 rounded-md text-xs">
                            🎁 Regalo
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1 bg-blue-600 text-white font-bold px-2 py-0.5 rounded-md text-[11px]">
                            🏷️ {offer.benefitDescription || 'Especial'}
                          </span>
                        )}
                      </div>
                    </div>

                    {discountAmount > 0 && (
                      <div className="text-[11px] font-semibold text-emerald-700 flex items-center gap-1">
                        <i className="fa-solid fa-piggy-bank"></i>
                        <span>Ahorrás $ {discountAmount.toLocaleString('es-AR')}</span>
                      </div>
                    )}
                  </div>

                  {/* Promo Code Box */}
                  <div className="mb-3">
                    <PromoCodeButton promoCode={offer.promoCode} onCopyNotice={onNotice} />
                  </div>

                  {/* Terms & Conditions */}
                  {offer.termsAndConditions && (
                    <div className="text-[11px] text-slate-500 bg-slate-50 p-2.5 rounded-xl border border-slate-100 mb-4 flex items-start gap-2">
                      <i className="fa-solid fa-circle-info text-slate-400 mt-0.5 shrink-0 text-xs"></i>
                      <span className="line-clamp-2" title={offer.termsAndConditions}>
                        {offer.termsAndConditions}
                      </span>
                    </div>
                  )}
                </div>

                {/* Bottom Action Row (Verified Direct Deep Link) */}
                <div className="pt-3 border-t border-slate-100 flex flex-col gap-2">
                  <a
                    href={offer.destinationUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    title={`Ir directamente a ${offer.destinationUrl}`}
                    className={`w-full text-center text-xs font-bold py-2.5 px-4 rounded-xl text-white transition-all shadow-xs flex items-center justify-center gap-1.5 ${
                      isMc
                        ? 'bg-red-600 hover:bg-red-700'
                        : isBk
                        ? 'bg-amber-700 hover:bg-amber-800'
                        : 'bg-rose-700 hover:bg-rose-800'
                    }`}
                  >
                    {offer.channel.includes('App') && !offer.channel.includes('Web') ? (
                      <>
                        <i className="fa-solid fa-mobile-screen-button text-[10px]"></i>
                        <span>Descargar App para esta Oferta ↗</span>
                      </>
                    ) : (
                      <>
                        <i className="fa-solid fa-arrow-up-right-from-square text-[10px]"></i>
                        <span>Ver Oferta en Web Oficial ↗</span>
                      </>
                    )}
                  </a>
                  <div className="flex items-center justify-between">
                    <span className="text-[10px] text-slate-400 truncate max-w-[180px]" title={offer.destinationUrl}>
                      🔗 {offer.destinationUrl.replace('https://', '').split('/')[0]}
                    </span>
                    <ReportButton onReportClick={() => onReportClick(offer)} />
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* 2. TABLE VIEW (Comparative Matrix) */}
      {viewMode === 'table' && (
        <div className="bg-white rounded-2xl shadow-xs border border-slate-200/80 overflow-hidden mb-12">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse text-xs sm:text-sm">
              <thead>
                <tr className="bg-slate-900 text-white text-xs font-bold uppercase tracking-wider">
                  <th className="py-4 px-5">Marca</th>
                  <th className="py-4 px-5">Producto & Categoría</th>
                  <th className="py-4 px-5 text-center">Beneficio</th>
                  <th className="py-4 px-5">Precio Normalizado</th>
                  <th className="py-4 px-5">Canal / Cupón</th>
                  <th className="py-4 px-5">Condiciones (IA)</th>
                  <th className="py-4 px-5 text-center">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {offers.map(offer => {
                  const isMc = offer.brand.includes("McDonald");
                  const isBk = offer.brand.includes("Burger");

                  return (
                    <tr key={offer.id} className="hover:bg-slate-50 transition">
                      <td className="py-4 px-5 whitespace-nowrap">
                        {isMc ? (
                          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-extrabold bg-red-600 text-yellow-300">
                            🍟 McDonald's
                          </span>
                        ) : isBk ? (
                          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-extrabold bg-amber-700 text-white">
                            👑 Burger King
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-extrabold bg-rose-700 text-white">
                            🍔 Mostaza
                          </span>
                        )}
                      </td>

                      <td className="py-4 px-5">
                        <div className="font-bold text-slate-900">{offer.title}</div>
                        <div className="text-[11px] text-slate-500 flex items-center gap-2 mt-0.5">
                          <span className="bg-slate-100 text-slate-600 px-1.5 py-0.2 rounded font-medium">
                            {offer.category}
                          </span>
                          <span>Vence: {new Date(offer.validUntil).toLocaleDateString('es-AR')}</span>
                        </div>
                      </td>

                      <td className="py-4 px-5 text-center whitespace-nowrap">
                        {offer.benefitType === 'TwoForOne' ? (
                          <span className="inline-block bg-purple-100 text-purple-700 font-extrabold px-2.5 py-1 rounded-full text-xs">
                            🔥 2x1
                          </span>
                        ) : offer.benefitType === 'PercentageDiscount' ? (
                          <span className="inline-block bg-emerald-100 text-emerald-700 font-extrabold px-2.5 py-1 rounded-full text-xs">
                            {offer.benefitDescription || '% OFF'}
                          </span>
                        ) : offer.benefitType === 'FreeGift' ? (
                          <span className="inline-block bg-amber-100 text-amber-700 font-extrabold px-2.5 py-1 rounded-full text-xs">
                            🎁 Gratis
                          </span>
                        ) : (
                          <span className="inline-block bg-blue-100 text-blue-700 font-semibold px-2.5 py-1 rounded-full text-xs">
                            🏷️ {offer.benefitDescription || 'Especial'}
                          </span>
                        )}
                      </td>

                      <td className="py-4 px-5 whitespace-nowrap">
                        {offer.normalizedPrice > 0 ? (
                          <div>
                            <div className="font-mono font-black text-slate-900 text-base">
                              $ {offer.normalizedPrice.toLocaleString('es-AR')}
                            </div>
                            {offer.originalPrice && offer.originalPrice > offer.normalizedPrice && (
                              <div className="text-xs text-slate-400 line-through font-mono">
                                $ {offer.originalPrice.toLocaleString('es-AR')}
                              </div>
                            )}
                          </div>
                        ) : (
                          <span className="text-xs font-black text-emerald-600 uppercase">Sin Cargo</span>
                        )}
                      </td>

                      <td className="py-4 px-5 whitespace-nowrap">
                        <div className="mb-1 text-xs">
                          <ChannelBadge channel={offer.channel} />
                        </div>
                        <PromoCodeButton promoCode={offer.promoCode} onCopyNotice={onNotice} />
                      </td>

                      <td className="py-4 px-5 text-xs text-slate-600 max-w-xs">
                        <p className="line-clamp-2" title={offer.termsAndConditions || ''}>
                          {offer.termsAndConditions || 'Consumo en local o take-away.'}
                        </p>
                      </td>

                      <td className="py-4 px-5 text-center whitespace-nowrap">
                        <div className="flex items-center justify-center gap-2">
                          <a
                            href={offer.destinationUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="inline-flex items-center gap-1 bg-slate-900 hover:bg-red-600 text-white text-xs font-bold px-3 py-1.5 rounded-lg transition"
                          >
                            <span>Ir Oficial</span>
                            <i className="fa-solid fa-arrow-up-right-from-square text-[10px]"></i>
                          </a>
                          <ReportButton onReportClick={() => onReportClick(offer)} />
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </>
  );
};
