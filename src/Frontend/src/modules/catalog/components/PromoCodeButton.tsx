import React, { useState } from 'react';

interface PromoCodeButtonProps {
  promoCode?: string | null;
  onCopyNotice?: (message: string) => void;
}

export const PromoCodeButton: React.FC<PromoCodeButtonProps> = ({ promoCode, onCopyNotice }) => {
  const [copied, setCopied] = useState<boolean>(false);

  if (!promoCode) {
    return <span className="text-xs text-slate-400 italic">No requiere cupón</span>;
  }

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(promoCode);
      setCopied(true);
      if (onCopyNotice) {
        onCopyNotice(`¡Código ${promoCode} copiado al portapapeles!`);
      }
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // Fallback
      if (onCopyNotice) onCopyNotice(`Código: ${promoCode}`);
    }
  };

  return (
    <button
      onClick={handleCopy}
      type="button"
      className={`inline-flex items-center gap-1.5 px-2.5 py-1 border rounded text-xs font-mono font-bold transition shadow-xs ${
        copied
          ? 'bg-emerald-50 text-emerald-700 border-emerald-300'
          : 'bg-amber-50 hover:bg-amber-100 text-amber-800 border-amber-300'
      }`}
      title="Copiar código al portapapeles"
    >
      <i className={copied ? 'fa-solid fa-check text-emerald-600' : 'fa-regular fa-copy'}></i>
      <span>{copied ? '¡Copiado!' : promoCode}</span>
    </button>
  );
};
