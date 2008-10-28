module Cpg
	class MathContext
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
			eval(@state[name])
		end

		def eval(s)
			begin
				return s unless s.is_a?(String)
				return Kernel.eval(s, binding)
			rescue Exception
				STDERR.puts("Error in MathContext: #{$!}\n\t#{$@.join("\n\t")}")
				return 0
			end
		end
	end
end
