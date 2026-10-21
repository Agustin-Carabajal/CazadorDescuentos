import React from 'react';

interface ChannelBadgeProps {
  channel: string;
}

export const ChannelBadge: React.FC<ChannelBadgeProps> = ({ channel }) => {
  if (channel === 'App') {
    return (
      <span className="inline-flex items-center gap-1 text-[11px] font-semibold px-2 py-0.5 rounded-md bg-purple-100 text-purple-700 border border-purple-200">
        <i className="fa-solid fa-mobile-screen"></i> Exclusivo App
      </span>
    );
  }

  if (channel === 'Web') {
    return (
      <span className="inline-flex items-center gap-1 text-[11px] font-semibold px-2 py-0.5 rounded-md bg-sky-100 text-sky-700 border border-sky-200">
        <i className="fa-solid fa-globe"></i> Exclusivo Web
      </span>
    );
  }

  return (
    <span className="inline-flex items-center gap-1 text-[11px] font-semibold px-2 py-0.5 rounded-md bg-emerald-100 text-emerald-700 border border-emerald-200">
      <i className="fa-solid fa-store"></i> App o Web
    </span>
  );
};
