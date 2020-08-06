import { Person } from './person';

export class SunAwardRecord {
    public Recipient: Person;
    public Supervisor: Person;
    public For: string;
    public Categories: number[];
    public CustomCategory: string;
}