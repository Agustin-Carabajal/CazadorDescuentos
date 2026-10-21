import React from 'react';

interface ReportButtonProps {
  onReportClick: () => void;
}

export const ReportButton: React.FC<ReportButtonProps> = ({ onReportClick }) => {
  return (
    <button
      onClick={onReportClick}
      type="button"
      title="Reportar oferta vencida o incorrecta"
      className="text-slate-400 hover:text-rose-600 p-1.5 rounded hover:bg-rose-50 transition"
    >
      <i className="fa-regular fa-flag"></i>
    </button>
  );
};
