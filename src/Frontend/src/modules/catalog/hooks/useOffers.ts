import { useState, useEffect, useCallback } from 'react';
import { catalogService, Offer, OfferFilter } from '../services/catalogService';

export function useOffers(initialFilter?: OfferFilter) {
  const [offers, setOffers] = useState<Offer[]>([]);
  const [filter, setFilter] = useState<OfferFilter>(initialFilter || { brand: 'all', sortBy: 'newest' });
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchOffers = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await catalogService.getOffers(filter);
      setOffers(data);
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Error desconocido al cargar las ofertas');
      }
    } finally {
      setLoading(false);
    }
  }, [filter]);

  useEffect(() => {
    fetchOffers();
  }, [fetchOffers]);

  const updateFilter = (newFilter: Partial<OfferFilter>) => {
    setFilter(prev => ({ ...prev, ...newFilter }));
  };

  return {
    offers,
    loading,
    error,
    filter,
    updateFilter,
    refetch: fetchOffers
  };
}
