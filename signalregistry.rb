module Cpg
	module SignalRegistry
		def initialize(*args)
			super
		
			@signal_registry = {}
		end
	
		def connect(obj, sign, how = :signal_connect)
			@signal_registry[obj] ||= []
			@signal_registry[obj] << obj.send(how, sign) { |*args| yield *args }
		end
	
		def disconnect(obj)
			(@signal_registry[obj] || []).each { |s| obj.signal_handler_disconnect(s) }
			@signal_registry.delete(obj)
		end
	
		def cleanup
			@signal_registry.each do |obj, signals|
				disconnect(obj)
			end
		
			@signal_registry.clear
		end
	end
end
