/**
 * Salesman Service Facade / Backward-Compatibility Export
 * Interface: src/app/services/interfaces/isalesman.service.ts
 * Implementation: src/app/services/implementations/salesman.service.ts
 * Repository: src/app/repositories/implementations/salesman.repository.ts
 */
export * from './interfaces/isalesman.service';
export * from './implementations/salesman.service';
export { SalesmanService as SalesmanApiService } from './implementations/salesman.service';
