import * as React from 'react';
import { Link, RouteComponentProps } from 'react-router-dom';
import { connect } from 'react-redux';
import { ApplicationState } from '../store';
import * as TSqlEditorStore from '../store/TSqlEditor';
import { withRouter } from 'react-router';

import "brace";
import "brace/mode/sqlserver";
import "brace/theme/sqlserver";
import "brace/theme/vibrant_ink";
import ReactAce from "react-ace";
import { Actions, State } from '../store/TSqlEditor';

interface TSqlEditorRouterProps {
    title: string;   
}

interface TSqlEditorProps extends RouteComponentProps<TSqlEditorRouterProps>, State.TSqlEditorState { }

type TSqlEditorDispatchProps = typeof TSqlEditorStore.actionCreators;

function unique<T>(vals: T[]): T[] {
    return Array.from(new Set(vals).keys());
}

class TSqlEditor extends React.Component<TSqlEditorProps & TSqlEditorDispatchProps, {}> {
    public render() {
        const { props } = this;
        return <div>
            {
                !!props.serviceInteraction
                    ? <div className="alert alert-info" role="alert">{props.serviceInteraction}...</div>
                    : displayOfLastCompilationResult()
            }
    
            <div className="btn-group">
                <button type="button" className="btn btn-primary" onClick={() => { props.requestTSqlCompile(props.sql); }}>Compile</button>

                <button type="button" className={`btn btn-${props.isLightTheme ? 'light' : 'dark'}`} onClick={() => { props.toggleTheme(); }}>
                    Theme: {props.isLightTheme ? "Light" : "Dark"}
                </button>

                <div className="dropdown btn-group">
                    <button className="btn btn-secondary dropdown-toggle" type="button" id="dropdownMenuButton" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                        {!props.exampleSqlFile ? "Load Example" : `Example: ${props.exampleSqlFile}`}
                    </button>
                    <div className="dropdown-menu" aria-labelledby="dropdownMenuButton">
                        {props.availableExampleSqlFiles.map((fileName) =>
                            <a key={fileName} className="dropdown-item" onClick={() => props.selectSqlFile(fileName)}>{fileName}</a>
                        )}
                    </div>
                </div>
            </div>

            <ReactAce                
                mode="sqlserver"
                theme={props.isLightTheme ? "sqlserver" : "vibrant_ink"}
                annotations={getAnnotations()}
                defaultValue="-- Enter SQL here. Click Compile to check it. Click 'Load Example' to... well... load an example."
                value={props.sql}
                onChange={(e) => props.updateSql(e)}
                width="900px"
                height="600px"
            />
        </div>;

        function displayOfLastCompilationResult() {
            const result: State.TSqlCompilationResult = props.lastCompilationResult;
            return result instanceof State.TSqlCompilationFailure
                ? <div className="alert alert-danger" role="alert">Error Talking to Server. Try again in a moment.</div>
                : result instanceof State.TSuccessfulSqlCompilationResult
                    ? <div className={`alert alert-${alertKind(result)}`} role="alert">Last compiled {result.compiledTime}. {resultDetails(result)}</div>
                    : <div className="alert alert-info" role="alert">Never Compiled</div>;
        }

        function resultDetails(compilationResult: State.TSuccessfulSqlCompilationResult): string {
            return compilationResult.issues.length === 0
                ? `No issues!`
                : `Issues identified on lines: ${unique(compilationResult.issues.map(issue => issue.startLine)).join(", ")}`
        }

        function alertKind(compilationResult: State.TSuccessfulSqlCompilationResult): string {
            return compilationResult.issues.some(it => it.issueLevel === State.IssueLevel.Error)
                ? "danger"
                : compilationResult.issues.some(it => it.issueLevel === State.IssueLevel.Warning)
                    ? "warning"
                    : "success";
        }
        
        function getAnnotations() {
            const { lastCompilationResult } = props;
            return (!State.isSqlCompilationResultSuccessful(lastCompilationResult))
                ? []
                : lastCompilationResult.issues.map(it => ({
                    row: it.startLine - 1,
                    column: it.startColumn - 1,
                    type: it.issueLevel === State.IssueLevel.Error ? "error" : "warning",
                    text: it.message
                }));
        }
    }
}

export default connect(
    (state: ApplicationState) => state.tsqlEditor, 
    TSqlEditorStore.actionCreators                 
)(TSqlEditor as any);

