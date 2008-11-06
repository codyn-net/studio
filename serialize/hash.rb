require 'serialize/serialize'
require 'serialize/object'

module Cpg::Serialize
	class Hash < ::Hash
		include Object
	
		def initialize(*args)
			super
			
			@order = []
			
			self.each { |k, v| @order << k }
		end
		
		def properties
			self.dup
		end
		
		def save_properties
			@order
		end
		
		def []=(key, val)
			super
			
			@order << key unless @order.include?(key)
		end
	
		def get_property(name)
			self[name]
		end
	
		def set_property(name, val)
			self[name] = val
			true
		end
	end
end

# ex:ts=4:noet:
