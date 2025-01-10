// enum-friendly-name.pipe.ts

import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'enumFriendlyName',
})
export class EnumFriendlyNamePipe implements PipeTransform {
    transform(value: string): string {
        if (!value) {
            return '';
        }
        
        // Insert space between lowercase and uppercase letters, and before sequences of uppercase followed by lowercase letters
        const result = value
            .replace(/([a-z])([A-Z])/g, '$1 $2') // Handles lowercase to uppercase transitions
            .replace(/([A-Z]+)([A-Z][a-z]+)/g, '$1 $2') // Handles sequences of capitals
            .trim();
        
        return result;
    }      
}
