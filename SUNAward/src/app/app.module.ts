import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { AppComponent } from './app.component';
import { HttpClientModule } from '@angular/common/http';
import { CheckboxListComponent } from './components/checkbox-list/checkbox-list.component';
import { FormsModule } from '@angular/forms';
import { AwardFormComponent } from './components/award-form/award-form.component';
import { TypeaheadSearchComponent } from './components/typeahead-search/typeahead-search.component';
import { PersonsService } from './services/persons.service';
import { AlertCloseableComponent } from './components/alert-closeable/alert-closeable.component';
import { PreviewAwardComponent } from './components/preview-award/preview-award.component';
import { RouterModule, Routes, RouteReuseStrategy } from '@angular/router';
import { ReuseStrategy } from './reuse-strategy';

const appRoutes: Routes = [
    { path: "", component: AwardFormComponent, pathMatch: "full" },
    { path: "preview", component: PreviewAwardComponent },
    { path: "**", redirectTo: "" }
];
@NgModule({
    declarations: [
        AppComponent,
        CheckboxListComponent,
        AwardFormComponent,
        TypeaheadSearchComponent,
        AlertCloseableComponent,
        PreviewAwardComponent,
    ],
    imports: [
        BrowserModule,
        HttpClientModule,
        FormsModule,
        NgbModule,
        RouterModule.forRoot(appRoutes),
    ],
    exports: [RouterModule],
    providers: [
        PersonsService,
        { provide: RouteReuseStrategy, useClass: ReuseStrategy }
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
