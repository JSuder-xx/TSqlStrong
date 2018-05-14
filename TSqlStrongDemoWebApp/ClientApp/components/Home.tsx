import * as React from 'react';
import { withRouter, RouteComponentProps } from 'react-router-dom';

export default class Home extends React.Component<RouteComponentProps<any>, {}> {
    public render() {
        return <div>
            <div>
                T-SQL Strong is a T-SQL type checker/verifier that
                <ul>
                    <li>Verifies the correctness of T-Sql code before it executes against a database.</li>
                    <li>
                        <span>Provides advanced type checking features such as</span>
                        <ul>
                            <li>Key Column Comparison - Protects against incorrect joins.</li>
                            <li>Null Type Checking - Guard against run-time errors of assigning a null value to a non-null column.</li>
                            <li>Check Constraints As Enumerations - Ensure only valid literals are assigned or compared against check constraint columns.</li>
                            <li>Insert Into Select Alias Matching - Protect against positional mistakes made with insert-select where you have many columns.</li>
                            <li>Verify the correctness of Temporary Table structure usage between different stored procedures.</li>
                            <li>VarChar size checks</li>
                            <li>Flow Typing. TSqlStrong can learn more about types (refinement) by analyzing flow control (IF/ELSEIF, CASE).</li>
                        </ul>
                    </li>
                </ul>
            </div>
            <p>
                Keep tabs on the project at <a target="_blank" href="https://github.com/JSuder-xx/TSqlStrong">GitHub</a>.
            </p>
            <div>
                <span>The project is currently a proof of concept. You can</span>
                <ul>
                    <li>Take it for a spin on the Try It Online tab right now.</li>
                    <li>Integrate into VS Code by building the project from GitHub and using the command line interface with the included VS Code build task.</li>
                </ul>
            </div>
            <h3>Next Steps</h3>
            <div>
                <ul>
                    <li>Improved type checking: Numeric data types (precision and scale). T-SQL Strong will check for both potential arithmetic overflows which cause a run-time error and also potential losses of precision when assigning one value to another.</li>
                    <li>Completely configurable errors. Each kind of validation can be set to Error, Warning, or Ignore to suit individual needs.</li>
                    <li>Schema importer that will load information about an existing database. When complete T-SQL strong can be used to validate code
                        <ul>
                            <li>On developer machines from VS Code and, eventually, SQL Server Management Studio and Visual Studio.</li>
                            <li>Integrated as part of Continuous Integration tooling. Ensure that all code (procs, functions, etc.) is valid against the database structure.</li>
                        </ul>
                    </li>
                </ul>                
                
            </div>
        </div>;
    }
}
