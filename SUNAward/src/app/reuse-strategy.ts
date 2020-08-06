import { RouteReuseStrategy, ActivatedRouteSnapshot, DetachedRouteHandle } from '@angular/router';
import { Injectable } from "@angular/core";

interface RouteStorageObject {
    snapshot: ActivatedRouteSnapshot;
    handle: DetachedRouteHandle;
}

@Injectable()
export class ReuseStrategy implements RouteReuseStrategy {
    private stored: { [key: string]: RouteStorageObject } = {};

    shouldDetach(route: ActivatedRouteSnapshot): boolean {
        // currently we only store the award form (we want to regenerate the preview from fresh state each time)
        return route.routeConfig.path === "";
    }

    store(route: ActivatedRouteSnapshot, handle: DetachedRouteHandle): void {
        this.stored[route.routeConfig.path] = { snapshot: route, handle: handle };
    }

    shouldAttach(route: ActivatedRouteSnapshot): boolean {
        return route.routeConfig.path in this.stored;
    }

    retrieve(route: ActivatedRouteSnapshot): DetachedRouteHandle {
        if (this.shouldAttach(route)) {
            return this.stored[route.routeConfig.path].handle;
        }

        return null;
    }

    shouldReuseRoute(future: ActivatedRouteSnapshot, curr: ActivatedRouteSnapshot): boolean {
        return future.routeConfig === curr.routeConfig;
    }

    clear(): void {
        this.stored = {};
    }
}