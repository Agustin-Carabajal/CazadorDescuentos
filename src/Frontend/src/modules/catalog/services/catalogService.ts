import { request } from '../../../shared/api/apiClient';

export interface Offer {
  id: number;
  brand: string;
  title: string;
  description: string;
  normalizedPrice: number;
  originalPrice?: number | null;
  benefitType: string;
  benefitDescription?: string | null;
  category: string;
  promoCode?: string | null;
  channel: string;
  destinationUrl: string;
  imageUrl?: string | null;
  termsAndConditions?: string | null;
  validUntil: string;
  isActive: boolean;
  reportCount: number;
}

export interface OfferFilter {
  brand?: string;
  benefitType?: string;
  category?: string;
  channel?: string;
  searchQuery?: string;
  sortBy?: string;
}

export const catalogService = {
  async getOffers(filter?: OfferFilter): Promise<Offer[]> {
    const params = new URLSearchParams();
    if (filter?.brand && filter.brand !== 'all') params.append('brand', filter.brand);
    if (filter?.benefitType && filter.benefitType !== 'all') params.append('benefitType', filter.benefitType);
    if (filter?.category && filter.category !== 'all') params.append('category', filter.category);
    if (filter?.channel && filter.channel !== 'all') params.append('channel', filter.channel);
    if (filter?.searchQuery) params.append('searchQuery', filter.searchQuery);
    if (filter?.sortBy) params.append('sortBy', filter.sortBy);

    const query = params.toString() ? `?${params.toString()}` : '';
    return request<Offer[]>(`/catalog/offers${query}`);
  },

  async getOfferById(id: number): Promise<Offer> {
    return request<Offer>(`/catalog/offers/${id}`);
  },

  async triggerScrape(): Promise<{ success: boolean; message: string; processedOffers: number }> {
    return request('/ingestion/trigger-scrape', { method: 'POST' });
  }
};
