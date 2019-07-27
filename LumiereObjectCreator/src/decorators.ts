
export function once(privateKey?: string | symbol | undefined) {
    return function(target: any, propertyKey: string, descriptor: PropertyDescriptor) {
        let getter = descriptor.get;
        if (!privateKey) {
            privateKey = Symbol(propertyKey);
        }

        descriptor.get = function() {
            return (this[privateKey] = this[privateKey] || getter.call(this));
        };
    };
}
