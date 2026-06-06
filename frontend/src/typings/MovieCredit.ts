import ModelBase from 'App/ModelBase';
import { Image } from 'Movie/Movie';

export type MovieCreditType = 'cast' | 'crew';

interface MovieCredit extends ModelBase {
  creditTmdbId: string;
  personTmdbId: number;
  personName: string;
  images: Image[];
  type: MovieCreditType;
  department: string;
  job: string;
  character: string;
  order: number;
}

export default MovieCredit;
