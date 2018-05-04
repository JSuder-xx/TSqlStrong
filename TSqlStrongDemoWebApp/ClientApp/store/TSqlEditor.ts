import { fetch, addTask } from 'domain-task';
import { Action, Reducer, ActionCreator } from 'redux';
import { AppThunkAction } from './';

export module State {
    export enum IssueLevel {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    export interface TSqlIssue {
        startLine: number;
        startColumn: number;
        endColumn: number;
        issueLevel: IssueLevel;
        message: string;
    }

    export interface TSuccessfulSqlCompilationResultJson {
        compiledTime: string;
        sql: string;
        compilationDurationMS: number;
        issues: TSqlIssue[];
    }

    export class TSuccessfulSqlCompilationResult {
        private _nominalTypingMarker: "TSuccessfulSqlCompilationResult" = "TSuccessfulSqlCompilationResult";
        constructor(
            public readonly compiledTime: string,
            public readonly sql: string,
            public readonly issues: TSqlIssue[],
            public readonly compilationDurationMS: number
        ) { }
    }

    export class TSqlCompilationFailure { }

    export class TSqlNeverCompiled { }

    export type TSqlCompilationResult = TSuccessfulSqlCompilationResult | TSqlCompilationFailure | TSqlNeverCompiled;

    export function isSqlCompilationResultSuccessful(result: TSqlCompilationResult): result is TSuccessfulSqlCompilationResult {
        return (result instanceof TSuccessfulSqlCompilationResult);
    }

    export type ServiceInteraction = '' | 'Compiling' | 'Load Example SQL';

    export interface TSqlEditorState {
        serviceInteraction: ServiceInteraction;
        sql: string;
        lastCompilationResult: TSqlCompilationResult;
        isLightTheme: boolean;
        exampleSqlFile: string;
        availableExampleSqlFiles: string[];
        exampleSqlFileContents: { [sqlFileName: string]: string };
    }

    export const originalState: TSqlEditorState = {
        serviceInteraction: "",
        sql: "",
        lastCompilationResult: new TSqlNeverCompiled(),
        isLightTheme: true,
        exampleSqlFile: "",
        availableExampleSqlFiles: [
            "foreign_key_column_comparison",
            "check_constraints",
            "null_type_checking",
            "insert_safety_with_alias",
            "procedure_calls_verifying_temp_table",
            "common_table_expressions",
            "cursors",
            "inline_table_value_functions",
            "while"
        ],
        exampleSqlFileContents: { }
    };
}

export module Actions {

    export interface UpdateSqlAction {
        type: 'UPDATE_SQL';
        sql: string;
    }

    export interface RequestTSqlCompileAction {
        type: 'REQUEST_COMPILE';
        sql: string;
    }

    export interface ToggleTheme {
        type: 'TOGGLE_THEME';
    }

    export interface ReceiveTSqlCompilationResultsAction {
        type: 'RECEIVE_TSQL_COMPILATION_RESULTS';
        compilationResult: State.TSuccessfulSqlCompilationResult;
    }

    export interface ReceiveExampleSql {
        type: 'RECEIVE_EXAMPLE_SQL';
        file: string;
        sql: string;
    }

    export interface UpdateExampleSql {
        type: 'UPDATE_EXAMPLE_SQL';
        file: string;
        sql: string;
    }

    export interface RequestExample {
        type: 'REQUEST_EXAMPLE';
    }

    export interface CompilationFailure {
        type: 'COMPILATION_FAILURE';
    }

    // Declare a 'discriminated union' type. This guarantees that all references to 'type' properties contain one of the
    // declared type strings (and not any other arbitrary string).
    export type KnownAction = UpdateSqlAction
        | RequestTSqlCompileAction | ReceiveTSqlCompilationResultsAction | CompilationFailure
        | ReceiveExampleSql | RequestExample | UpdateExampleSql
        | ToggleTheme;

}

export const actionCreators = {
    updateSql: (sql: string): AppThunkAction<Actions.KnownAction> => (dispatch, getState) => {
        dispatch({ type: 'UPDATE_SQL', sql })
    },
    toggleTheme: (): AppThunkAction<Actions.KnownAction> => (dispatch, getState) => {
        dispatch({ type: 'TOGGLE_THEME' })
    },
    selectSqlFile: (file: string): AppThunkAction<Actions.KnownAction> => (dispatch, getState) => {
        const { tsqlEditor } = getState();
        const sql = tsqlEditor.exampleSqlFileContents[file];
        if (!!sql)
            dispatch({ type: 'UPDATE_EXAMPLE_SQL', file, sql });
        else {
            addTask(fetch(`sql/${file}.sql`)
                .then((response: Response) => response.text())
                .then(data => {
                    dispatch({ type: 'RECEIVE_EXAMPLE_SQL', file, sql: data });
                })
            );
            dispatch({ type: 'REQUEST_EXAMPLE' });
        }
    },
    requestTSqlCompile: (sql: string): AppThunkAction<Actions.KnownAction> => (dispatch, getState) => {
        // if we have already compiled this then bail
        const { lastCompilationResult } = getState().tsqlEditor;
        if (State.isSqlCompilationResultSuccessful(lastCompilationResult) && lastCompilationResult.sql.trim() === (sql || "").trim())
            return;

        addTask(fetch(
            `api/TSqlStrongCompiler/Compile`,
            {
                method: "POST",
                body: JSON.stringify({ sql }),
                headers: {
                    'Content-Type': 'application/json; charset=utf-8',
                    'Accept': 'application/json'
                }
            })
            .then(response => response.json() as Promise<State.TSuccessfulSqlCompilationResultJson>)
            .then(json => {
                dispatch({
                    type: 'RECEIVE_TSQL_COMPILATION_RESULTS',
                    compilationResult: new State.TSuccessfulSqlCompilationResult(
                        json.compiledTime,
                        json.sql,
                        json.issues,
                        json.compilationDurationMS
                    )
                });
            })
            .catch(() => {
                dispatch({ type: 'COMPILATION_FAILURE' });
            })
        ); 
        dispatch({ type: 'REQUEST_COMPILE', sql });
    }
};

function singleKeyValueObject({ key, value }: { key: string; value: any }): any {
    const obj: { [index: string]: any } = { };
    obj[key] = value;
    return obj;
}

export const reducer: Reducer<State.TSqlEditorState> = (state: State.TSqlEditorState | undefined, incomingAction: Action): State.TSqlEditorState => {
    if (!state)
        return State.originalState;

    const action = incomingAction as Actions.KnownAction;
    switch (action.type) {
        case 'UPDATE_SQL':
            return {
                ...state,
                sql: action.sql
            };
        case 'UPDATE_EXAMPLE_SQL':
            return {
                ...state,
                sql: action.sql,
                exampleSqlFile: action.file,
                lastCompilationResult: new State.TSqlNeverCompiled()
            };
        case 'REQUEST_COMPILE':
            return {
                ...state,
                serviceInteraction: "Compiling"
            };
        case 'RECEIVE_EXAMPLE_SQL':
            return {
                ...state,
                serviceInteraction: "",
                exampleSqlFileContents: {
                    ...state.exampleSqlFileContents,
                    ...singleKeyValueObject({ key: action.file, value: action.sql })
                },
                exampleSqlFile: action.file,
                sql: action.sql,
                lastCompilationResult: new State.TSqlNeverCompiled()
            };
        case 'REQUEST_EXAMPLE':
            return {
                ...state,
                serviceInteraction: "Load Example SQL"
            };
        case 'RECEIVE_TSQL_COMPILATION_RESULTS':
            return {
                ...state,
                serviceInteraction: "",
                sql: state.sql,
                lastCompilationResult: action.compilationResult
            };
        case 'TOGGLE_THEME':
            return {
                ...state,
                isLightTheme: !state.isLightTheme
            };
        case 'COMPILATION_FAILURE':
            return {
                ...state,
                serviceInteraction: "",
                lastCompilationResult: new State.TSqlCompilationFailure()
            }
        default:
            return state;
    }
};
