import React, { useState } from 'react';
import { reportService } from '../services/reportService';
import { Offer } from '../../catalog/services/catalogService';

interface ReportModalProps {
  offer: Offer | null;
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (message: string) => void;
}

export const ReportModal: React.FC<ReportModalProps> = ({ offer, isOpen, onClose, onSuccess }) => {
  const [reason, setReason] = useState<string>('Expired');
  const [comments, setComments] = useState<string>('');
  const [submitting, setSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  if (!isOpen || !offer) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setErrorMessage(null);

    try {
      const response = await reportService.submitReport({
        offerId: offer.id,
        reason,
        comments
      });

      if (response.success) {
        onSuccess(response.message);
        onClose();
      } else {
        setErrorMessage(response.message || 'Error al enviar el reporte.');
      }
    } catch (err: unknown) {
      if (err instanceof Error) {
        setErrorMessage(err.message);
      } else {
        setErrorMessage('Error de conexión al enviar reporte.');
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-xs z-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl max-w-md w-full shadow-2xl overflow-hidden border border-slate-200">
        <div className="bg-rose-600 text-white p-4 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <i className="fa-solid fa-triangle-exclamation"></i>
            <h3 className="font-bold text-base">Reportar Oferta</h3>
          </div>
          <button onClick={onClose} type="button" className="text-white/80 hover:text-white text-lg">
            &times;
          </button>
        </div>

        <div className="p-6">
          <p className="text-xs text-slate-500 mb-1">Oferta seleccionada:</p>
          <div className="font-bold text-slate-800 text-sm mb-4">
            [{offer.brand}] {offer.title}
          </div>

          {errorMessage && (
            <div className="mb-4 p-3 bg-rose-50 border border-rose-200 text-rose-700 text-xs rounded-lg">
              {errorMessage}
            </div>
          )}

          <form onSubmit={handleSubmit}>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Motivo del reporte *</label>
            <select
              value={reason}
              onChange={e => setReason(e.target.value)}
              required
              className="w-full text-sm bg-slate-50 border border-slate-300 rounded-lg p-2.5 mb-4 focus:outline-none focus:ring-2 focus:ring-rose-500"
            >
              <option value="Expired">Oferta ya vencida o no disponible</option>
              <option value="WrongPrice">Precio incorrecto o diferente al pagar</option>
              <option value="BrokenLink">Enlace o cupón roto</option>
              <option value="MisleadingTerms">Condiciones o restricciones confusas</option>
              <option value="Other">Otro problema</option>
            </select>

            <label className="block text-xs font-semibold text-slate-700 mb-1">Detalle o comentario adicional (opcional)</label>
            <textarea
              value={comments}
              onChange={e => setComments(e.target.value)}
              rows={3}
              placeholder="Describe brevemente lo ocurrido..."
              className="w-full text-sm bg-slate-50 border border-slate-300 rounded-lg p-2.5 mb-6 focus:outline-none focus:ring-2 focus:ring-rose-500"
            ></textarea>

            <div className="flex justify-end gap-3">
              <button
                type="button"
                onClick={onClose}
                disabled={submitting}
                className="px-4 py-2 text-sm text-slate-600 hover:bg-slate-100 rounded-lg font-medium transition"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="px-5 py-2 text-sm bg-rose-600 hover:bg-rose-700 text-white font-bold rounded-lg shadow-sm transition disabled:opacity-50"
              >
                {submitting ? 'Enviando...' : 'Enviar Reporte'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};
