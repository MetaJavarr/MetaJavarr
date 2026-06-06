import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import ImportList from 'typings/ImportList';

function getFieldValue(importList: ImportList, name: string) {
  return importList.fields.find((field) => field.name === name)?.value;
}

function createMovieCreditImportListSelector(
  actorProvider?: string,
  actorId?: string
) {
  return createSelector(
    (state: AppState) => state.settings.importLists.items,
    (importLists) => {
      if (!actorProvider || !actorId) {
        return undefined;
      }

      const importListIds = importLists.reduce(
        (acc: ImportList[], importList) => {
          if (importList.implementation === 'MetaTubeActressImport') {
            const providerValue = getFieldValue(importList, 'provider');
            const actorIdValue = getFieldValue(importList, 'actorId');

            if (
              typeof providerValue === 'string' &&
              typeof actorIdValue === 'string' &&
              providerValue.toLowerCase() === actorProvider.toLowerCase() &&
              actorIdValue.toLowerCase() === actorId.toLowerCase()
            ) {
              acc.push(importList);

              return acc;
            }
          }

          return acc;
        },
        []
      );

      if (importListIds.length === 0) {
        return undefined;
      }

      return importListIds[0];
    }
  );
}

export default createMovieCreditImportListSelector;
