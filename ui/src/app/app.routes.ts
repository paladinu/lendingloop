import { Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard';
import { AuthLayoutComponent } from './components/auth-layout/auth-layout.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { EmailVerificationComponent } from './components/email-verification/email-verification.component';
import { MainComponent } from './components/main/main.component';
import { LoopListComponent } from './components/loop-list/loop-list.component';
import { LoopCreateComponent } from './components/loop-create/loop-create.component';
import { LoopDetailComponent } from './components/loop-detail/loop-detail.component';
import { LoopInviteComponent } from './components/loop-invite/loop-invite.component';
import { LoopInvitationsComponent } from './components/loop-invitations/loop-invitations.component';
import { LoopMembersComponent } from './components/loop-members/loop-members.component';
import { AcceptInvitationComponent } from './components/accept-invitation/accept-invitation.component';
import { ItemVisibilityComponent } from './components/item-visibility/item-visibility.component';
import { ItemAddComponent } from './components/item-add/item-add.component';
import { ItemEditComponent } from './components/item-edit/item-edit.component';
import { ItemRequestListComponent } from './components/item-request-list/item-request-list.component';
import { MyRequestsComponent } from './components/my-requests/my-requests.component';
import { PublicLoopsComponent } from './components/public-loops/public-loops.component';
import { ArchivedLoopsComponent } from './components/archived-loops/archived-loops.component';
import { LoopSettingsComponent } from './components/loop-settings/loop-settings.component';

export const routes: Routes = [
    // Authentication routes (no guard needed)
    {
        path: 'auth',
        component: AuthLayoutComponent,
        children: [
            { path: 'login', component: LoginComponent },
            { path: 'register', component: RegisterComponent },
            { path: 'verify-email', component: EmailVerificationComponent },
            { path: '', redirectTo: 'login', pathMatch: 'full' }
        ]
    },

    // Convenience routes for direct access
    { path: 'login', redirectTo: 'auth/login', pathMatch: 'full' },
    { path: 'register', redirectTo: 'auth/register', pathMatch: 'full' },
    { path: 'verify-email', redirectTo: 'auth/verify-email', pathMatch: 'full' },

    // Loop routes (protected)
    {
        path: 'loops',
        canActivate: [AuthGuard],
        children: [
            { path: '', component: LoopListComponent },
            { path: 'create', component: LoopCreateComponent },
            { path: 'invitations', component: LoopInvitationsComponent },
            { path: 'accept-invitation', component: AcceptInvitationComponent },
            { path: 'public', component: PublicLoopsComponent },
            { path: 'archived', component: ArchivedLoopsComponent },
            { path: ':id', component: LoopDetailComponent },
            { path: ':id/invite', component: LoopInviteComponent },
            { path: ':id/members', component: LoopMembersComponent },
            { path: ':id/settings', component: LoopSettingsComponent }
        ]
    },

    // Item routes (protected)
    {
        path: 'items/add',
        component: ItemAddComponent,
        canActivate: [AuthGuard]
    },
    {
        path: 'items/:id/edit',
        component: ItemEditComponent,
        canActivate: [AuthGuard]
    },
    {
        path: 'items/:id/visibility',
        component: ItemVisibilityComponent,
        canActivate: [AuthGuard]
    },

    // Item Request routes (protected)
    {
        path: 'requests',
        canActivate: [AuthGuard],
        children: [
            { path: '', component: ItemRequestListComponent },
            { path: 'my-requests', component: MyRequestsComponent }
        ]
    },

    // Protected main app route
    {
        path: '',
        component: MainComponent,
        canActivate: [AuthGuard]
    },

    // Fallback route
    { path: '**', redirectTo: 'auth/login' }
];
