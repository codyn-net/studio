module Cpg
	class MathContext
		class Wrapper
			def initialize(context, proxy)
				@context = context
				@proxy = proxy
			end
			
			def method_missing(name, *args)
				@context.eval(@proxy.get_property(name))
			end
		end
		
		include Math

		def initialize(*vars)
			@state = {}
			vars.each { |h| @state.merge!(h) }

			#state.each do |k, v|
			#	instance_variable_set("@#{k}", v)

			#	(class << self; self; end).class_eval do
			#		define_method(k) { eval(instance_variable_get("@#{k}")) }
			#	end
			#end
		end
		
		def method_missing(name, *args)
			s = @state[name]
			
			if s.is_a?(Components::SimulatedObject)
				Wrapper.new(self, s)
			else
				eval(s)
			end
		end

		def eval(s)
			begin
				return s unless s.is_a?(String)
				return Kernel.eval(s, binding)
			rescue Exception
				STDERR.puts("Error in MathContext while evaluating `#{s}': #{$!}\n\t#{$@.join("\n\t")}")
				return 0
			end
		end
	end
end
