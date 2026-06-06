import React, { useCallback, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import {
  selectImportListSchema,
  setImportListFieldValue,
  setImportListValue,
} from 'Store/Actions/settingsActions';
import createMovieCreditImportListSelector from 'Store/Selectors/createMovieCreditImportListSelector';
import { MovieCastPosterProps } from './Cast/MovieCastPoster';
import { MovieCrewPosterProps } from './Crew/MovieCrewPoster';

type MovieCreditPosterProps = {
  component: React.ElementType;
  creditTmdbId: string;
} & (
  | Omit<MovieCrewPosterProps, 'canFollow' | 'onImportListSelect'>
  | Omit<MovieCastPosterProps, 'canFollow' | 'onImportListSelect'>
);

function parseActorIdentity(creditTmdbId: string) {
  const parts = creditTmdbId.split(':');
  const actorSegmentIndex = parts.indexOf('actor');

  if (
    actorSegmentIndex === -1 ||
    actorSegmentIndex + 2 >= parts.length ||
    parts[actorSegmentIndex + 1] === 'unresolved'
  ) {
    return undefined;
  }

  return {
    provider: parts[actorSegmentIndex + 1],
    actorId: parts.slice(actorSegmentIndex + 2).join(':'),
  };
}

function MovieCreditPoster({
  component: ItemComponent,
  creditTmdbId,
  personName,
  ...otherProps
}: MovieCreditPosterProps) {
  const actorIdentity = useMemo(
    () => parseActorIdentity(creditTmdbId),
    [creditTmdbId]
  );

  const importList = useSelector(
    createMovieCreditImportListSelector(
      actorIdentity?.provider,
      actorIdentity?.actorId
    )
  );

  const dispatch = useDispatch();

  const handleImportListSelect = useCallback(() => {
    if (!actorIdentity) {
      return;
    }

    dispatch(
      selectImportListSchema({
        implementation: 'MetaTubeActressImport',
        implementationName: 'MetaTube Actress',
        presetName: undefined,
      })
    );

    dispatch(
      // @ts-expect-error 'setImportListFieldValue' isn't typed yet
      setImportListFieldValue({ name: 'actressName', value: personName })
    );

    dispatch(
      // @ts-expect-error 'setImportListFieldValue' isn't typed yet
      setImportListFieldValue({
        name: 'provider',
        value: actorIdentity.provider,
      })
    );

    dispatch(
      // @ts-expect-error 'setImportListFieldValue' isn't typed yet
      setImportListFieldValue({
        name: 'actorId',
        value: actorIdentity.actorId,
      })
    );

    dispatch(
      // @ts-expect-error 'setImportListValue' isn't typed yet
      setImportListValue({
        name: 'name',
        value: `${personName} - ${actorIdentity.provider}:${actorIdentity.actorId}`,
      })
    );
  }, [actorIdentity, dispatch, personName]);

  return (
    <ItemComponent
      {...otherProps}
      personName={personName}
      canFollow={!!actorIdentity}
      importList={importList}
      onImportListSelect={handleImportListSelect}
    />
  );
}

export default MovieCreditPoster;
