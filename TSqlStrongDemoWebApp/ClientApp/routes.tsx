import * as React from 'react';
import { Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import Home from './components/Home';
import TSqlEditor from './components/TSqlEditor';

export const routes = <Layout>
    <Route exact path='/' component={ Home as any } />
    <Route path='/tsqleditor' component={TSqlEditor as any} />
</Layout>;
