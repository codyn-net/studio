require 'serialize/serialize'
require 'serialize/object'

module Cpg::Serialize
	class Array < ::Array
		include Object
		
		class Properties
			def initialize(what)
				@what = what.dup
				@what.delete_if { |x| x.is_a?(Object) }
			end
			
			def include?(a)
				@what.include?(a)
			end
			
			def each
				@what.each { |x| yield x, '' }
			end
			
			def [](v)
				''
			end
			
			def keys
				@what
			end
		end
			
		def properties
			Properties.new(self)
		end
		
		def property_set?(prop)
			self.include?(prop)
		end
	
		def set_property(name, val)
			self << name unless self.include?(name)
			true
		end
	
		def get_property(name)
			''
		end
	end
end

# ex:ts=4:noet:
